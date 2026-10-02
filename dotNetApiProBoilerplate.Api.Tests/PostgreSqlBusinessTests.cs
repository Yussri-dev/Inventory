using System.Net;
using System.Net.Http.Json;
using Inventory.Domain.Entities;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CashSessions.Requests;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Tests;

public sealed partial class PostgreSqlSyncTests
{
    private async Task PostOk(SeedData data,string path,object payload)
    {
        using var response=await fixture.Send(data,path,payload);
        Assert.True(response.IsSuccessStatusCode,$"{path}: HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    private async Task<Guid> Customer(SeedData data,decimal balance=0)
    {
        await using var db=fixture.Database();
        var customer=new Customer {Id=Guid.NewGuid(),TenantId=data.TenantId,Name="Business test",
            CurrentBalance=balance,AllowCredit=true,CreditLimit=1000,IsActive=true};
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer.Id;
    }

    private async Task<(Guid SaleId,Guid LineId)> Sold(SeedData data)
    {
        var result=await fixture.Upload(data,data.Request());
        Assert.Equal("Done",result.Items.Single().Status);
        await using var db=fixture.Database();
        var line=await db.SaleLines.SingleAsync(x=>x.TenantId==data.TenantId);
        return (line.SaleId,line.Id);
    }

    private static CreateCompleteReturnRequest ReturnRequest(SeedData data,Guid sale,Guid line,decimal quantity=1)=>new() {
        ClientOperationId=Guid.NewGuid(),SaleId=sale,CashSessionId=data.CashId,RefundType=RefundMethod.Cash,
        Lines=new(){new(){SaleLineId=line,ProductId=data.ProductId,Quantity=quantity,UnitPrice=10,Reason="Test return",RestockItem=true}}
    };

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(18)]
    public async Task Purchase_increases_stock_and_supplier_debt_once(int paid)
    {
        var data=await fixture.Seed();
        await using var db=fixture.Database();
        var supplier=new Supplier {Id=Guid.NewGuid(),TenantId=data.TenantId,Name="Test supplier",IsActive=true};
        db.Suppliers.Add(supplier); await db.SaveChangesAsync();
        var request=new CreateCompletePurchaseRequest {ClientOperationId=Guid.NewGuid(),SupplierId=supplier.Id,
            Lines=new(){new(){ProductId=data.ProductId,Quantity=3,UnitPrice=5,VatRate=20}}};
        if (paid > 0) request.Payment = new() { Amount = paid, PaymentMethod = "Card" };
        await PostOk(data,"/api/v1/purchases/complete",request);
        await PostOk(data,"/api/v1/purchases/complete",request);
        db.ChangeTracker.Clear();
        Assert.Equal(13,(await db.Stocks.SingleAsync(x=>x.TenantId==data.TenantId)).Quantity);
        Assert.Single(await db.Purchases.Where(x=>x.TenantId==data.TenantId).ToListAsync());
        Assert.Equal(18-paid,(await db.Suppliers.SingleAsync(x=>x.Id==supplier.Id)).CurrentBalance);
        var transactions = await db.SupplierTransactions.Where(x=>x.TenantId==data.TenantId).ToListAsync();
        Assert.Equal(18, transactions.Single(x=>x.Type==SupplierTransactionType.Purchase).Amount);
        Assert.Equal(paid > 0 ? 2 : 1, transactions.Count);
        if (paid > 0) Assert.Equal(paid, transactions.Single(x=>x.Type==SupplierTransactionType.Payment).Amount);
    }

    [Fact]
    public async Task Cash_purchase_without_funds_leaves_no_partial_purchase_or_stock()
    {
        var data=await fixture.Seed();
        await using var db=fixture.Database();
        var supplier=new Supplier {Id=Guid.NewGuid(),TenantId=data.TenantId,Name="Supplier",IsActive=true};
        db.Add(supplier); await db.SaveChangesAsync();
        var request=new CreateCompletePurchaseRequest {ClientOperationId=Guid.NewGuid(),SupplierId=supplier.Id,
            Lines=new(){new(){ProductId=data.ProductId,Quantity=3,UnitPrice=5}},
            Payment=new(){Amount=15,PaymentMethod="Cash"}};
        using var response=await fixture.Send(data,"/api/v1/purchases/complete",request);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Purchases.Where(x=>x.TenantId==data.TenantId).ToListAsync());
        Assert.Equal(10,(await db.Stocks.SingleAsync(x=>x.TenantId==data.TenantId)).Quantity);
    }

    [Fact]
    public async Task Return_restocks_refunds_once_and_cash_closure_matches()
    {
        var data=await fixture.Seed();
        var sale=await Sold(data);
        var request=ReturnRequest(data,sale.SaleId,sale.LineId);
        await PostOk(data,"/api/v1/returns/complete",request);
        await PostOk(data,"/api/v1/returns/complete",request);
        await PostOk(data,$"/api/v1/cashsessions/{data.CashId}/close",new CloseCashSessionRequest {ActualCash=10});
        await using var db=fixture.Database();
        Assert.Equal(9,(await db.Stocks.SingleAsync(x=>x.TenantId==data.TenantId)).Quantity);
        Assert.Single(await db.Returns.Where(x=>x.TenantId==data.TenantId).ToListAsync());
        var session=await db.CashSessions.SingleAsync(x=>x.Id==data.CashId);
        Assert.Equal(10,session.ClosingAmountExpected);
        Assert.Equal(0,session.Difference);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Return_cannot_exceed_sold_quantity_in_one_or_multiple_requests(bool split)
    {
        var data=await fixture.Seed();
        var sale=await Sold(data);
        if(split) await PostOk(data,"/api/v1/returns/complete",ReturnRequest(data,sale.SaleId,sale.LineId,1));
        var request=ReturnRequest(data,sale.SaleId,sale.LineId,split?2:3);
        request.RefundType=RefundMethod.Card; // Cash availability must not be the reason for rejection.
        using var response=await fixture.Send(data,"/api/v1/returns/complete",request);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        await using var db=fixture.Database();
        Assert.Equal(split?9:8,(await db.Stocks.SingleAsync(x=>x.TenantId==data.TenantId)).Quantity);
    }

    [Fact]
    public async Task Return_cannot_refund_an_arbitrary_price()
    {
        var data=await fixture.Seed();
        var sale=await Sold(data);
        var request=ReturnRequest(data,sale.SaleId,sale.LineId);
        request.Lines[0].UnitPrice=1000;
        request.RefundType=RefundMethod.Card;
        using var response=await fixture.Send(data,"/api/v1/returns/complete",request);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        await using var db=fixture.Database();
        Assert.Empty(await db.Returns.Where(x=>x.TenantId==data.TenantId).ToListAsync());
    }

    [Fact]
    public async Task Return_duplicate_lines_cannot_bypass_remaining_quantity()
    {
        var data = await fixture.Seed();
        var sale = await Sold(data);
        var request = ReturnRequest(data, sale.SaleId, sale.LineId, 2);
        request.Lines.Add(ReturnRequest(data, sale.SaleId, sale.LineId).Lines[0]);
        request.RefundType = RefundMethod.Card;
        using var response = await fixture.Send(data, "/api/v1/returns/complete", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = fixture.Database();
        Assert.Empty(await db.Returns.Where(x => x.TenantId == data.TenantId).ToListAsync());
    }

    [Fact]
    public async Task Return_uses_original_inventory_conversion()
    {
        var data = await fixture.Seed();
        var sale = await Sold(data);
        await using var db = fixture.Database();
        // Historical receipt: two sold packs represent twelve inventory units.
        var line = await db.SaleLines.SingleAsync(x => x.Id == sale.LineId);
        line.UnitQuantity = 12;
        await db.SaveChangesAsync();
        await PostOk(data, "/api/v1/returns/complete", ReturnRequest(data, sale.SaleId, sale.LineId));
        db.ChangeTracker.Clear();
        Assert.Equal(14, (await db.Stocks.SingleAsync(x => x.TenantId == data.TenantId)).Quantity);
        Assert.Equal(6, (await db.StockMovements.SingleAsync(x => x.TenantId == data.TenantId && x.Type == StockMovementType.Return)).QuantityChange);
    }

    [Fact]
    public async Task Customer_payment_reduces_debt_once_and_cash_is_counted()
    {
        var data=await fixture.Seed();
        var customerId=await Customer(data,30);
        var request=new RegisterCustomerPaymentRequest {ClientOperationId=Guid.NewGuid(),CustomerId=customerId,
            Amount=10,IsCash=true,CashSessionId=data.CashId};
        await PostOk(data,"/api/v1/customertransactions/register-payment",request);
        await PostOk(data,"/api/v1/customertransactions/register-payment",request);
        await PostOk(data,$"/api/v1/cashsessions/{data.CashId}/close",new CloseCashSessionRequest {ActualCash=10});
        await using var db=fixture.Database();
        Assert.Equal(20,(await db.Customers.SingleAsync(x=>x.Id==customerId)).CurrentBalance);
        Assert.Single(await db.CustomerTransactions.Where(x=>x.TenantId==data.TenantId).ToListAsync());
        Assert.Single(await db.CashMovements.Where(x=>x.TenantId==data.TenantId).ToListAsync());
        Assert.Equal(10,(await db.CashSessions.SingleAsync(x=>x.Id==data.CashId)).ClosingAmountExpected);
    }

    [Fact]
    public async Task Customer_overpayment_is_rejected_without_changing_balance()
    {
        var data=await fixture.Seed();
        var id=await Customer(data,10);
        using var response=await fixture.Send(data,"/api/v1/customertransactions/register-payment",
            new RegisterCustomerPaymentRequest {ClientOperationId=Guid.NewGuid(),CustomerId=id,Amount=11,IsCash=false});
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        await using var db=fixture.Database();
        Assert.Equal(10,(await db.Customers.SingleAsync(x=>x.Id==id)).CurrentBalance);
        Assert.Empty(await db.CustomerTransactions.Where(x=>x.TenantId==data.TenantId).ToListAsync());
    }

    [Fact]
    public async Task Customer_credit_refund_reduces_credit_once()
    {
        var data=await fixture.Seed();
        var id=await Customer(data,-20);
        var request=new RegisterCustomerRefundRequest {ClientOperationId=Guid.NewGuid(),CustomerId=id,Amount=5,IsCash=false};
        await PostOk(data,"/api/v1/customertransactions/register-refund",request);
        await PostOk(data,"/api/v1/customertransactions/register-refund",request);
        await using var db=fixture.Database();
        Assert.Equal(-15,(await db.Customers.SingleAsync(x=>x.Id==id)).CurrentBalance);
        Assert.Single(await db.CustomerTransactions.Where(x=>x.TenantId==data.TenantId).ToListAsync());
    }
}
