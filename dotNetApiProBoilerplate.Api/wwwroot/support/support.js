'use strict';
const $ = id => document.getElementById(id);
let token = null, selected = null, pending = null, busy = false;
const money = value => new Intl.NumberFormat('fr-FR', {minimumFractionDigits:2, maximumFractionDigits:2}).format(value);
function message(text, error = false) { $('message').textContent = text; $('message').classList.toggle('error', error); }
function resetCorrection() { selected = pending = null; $('correctionPanel').hidden = true; $('review').hidden = true; $('correction').hidden = false; }
function logout() { token = null; resetCorrection(); $('workspace').hidden = true; $('logout').hidden = true; $('loginPanel').hidden = false; $('sales').replaceChildren(); $('history').replaceChildren(); }
async function api(path, body) {
  const response = await fetch(path, {method:body === undefined ? 'GET':'POST', cache:'no-store', headers:{...(token ? {Authorization:`Bearer ${token}`} : {}), ...(body === undefined ? {} : {'Content-Type':'application/json'})}, ...(body === undefined ? {} : {body:JSON.stringify(body)})});
  const data = await response.json().catch(() => ({}));
  if (!response.ok) {
    if (response.status === 401) logout();
    throw new Error(data.detail || data.title || (response.status === 403 ? 'Accès réservé aux administrateurs autorisés.' : `Erreur ${response.status}. Reconnectez-vous si votre session a expiré.`));
  }
  return data;
}
function scoped(path) { return `/api/support/${encodeURIComponent($('store').value)}/${path}`; }
function row(parent, values) { const tr=document.createElement('tr'); for(const value of values){const td=document.createElement('td'); if(value instanceof Node)td.append(value);else td.textContent=value ?? '';tr.append(td);}parent.append(tr); }
async function sales() {
  resetCorrection(); const list = await api(scoped(`sales?search=${encodeURIComponent($('saleQuery').value)}`)); $('sales').replaceChildren();
  for(const sale of list){const button=document.createElement('button');button.textContent='Corriger le client';button.onclick=()=>run(async()=>{
    resetCorrection();selected=sale;$('selectedSale').textContent=`Ticket ${sale.invoiceNumber} · ${sale.customerName} · ${money(sale.totalAmount)}`;
    $('reason').value='';$('customerQuery').value='';$('correctionPanel').hidden=false;await customers();$('correctionPanel').scrollIntoView({behavior:'smooth'});
  });row($('sales'),[sale.invoiceNumber,new Date(sale.saleDate).toLocaleString('fr-FR'),sale.customerName,money(sale.totalAmount),button]);}
  if(!list.length)row($('sales'),['Aucune vente correspondante.']);
}
async function customers(){const list=await api(scoped(`customers?search=${encodeURIComponent($('customerQuery').value)}`));$('customer').replaceChildren(new Option('Sélectionner le bon client',''));for(const customer of list)$('customer').add(new Option(`${customer.name}${customer.phone ? ' · '+customer.phone : ''} · Solde ${money(customer.currentBalance)}`,customer.id));}
async function history(){const list=await api(scoped('corrections'));$('history').replaceChildren();for(const item of list)row($('history'),[new Date(item.createdAt).toLocaleString('fr-FR'),item.invoiceNumber,item.reason,money(item.transferredDebt),item.id]);if(!list.length)row($('history'),['Aucune correction enregistrée.']);}
async function run(action){if(busy)return;busy=true;document.querySelectorAll('button').forEach(b=>b.disabled=true);try{await action();}catch(error){message(error.message,true);}finally{busy=false;document.querySelectorAll('button').forEach(b=>b.disabled=false);}}
$('login').onsubmit=e=>{e.preventDefault();run(async()=>{const auth=await api('/api/auth/login',{email:$('email').value,password:$('password').value});$('password').value='';if(!['Admin','SuperAdmin'].includes(auth.role))throw new Error('Ce compte ne dispose pas des droits administrateur.');token=auth.accessToken;const stores=await api('/api/support/stores');$('store').replaceChildren();for(const store of stores)$('store').add(new Option(store.name,store.id));$('loginPanel').hidden=true;$('workspace').hidden=false;$('logout').hidden=false;if(stores.length){await sales();await history();message('Sélectionnez une vente dans le magasin concerné.');}else message('Aucun magasin accessible.');});};
$('logout').onclick=()=>{logout();message('Vous êtes déconnecté.');};
$('store').onchange=()=>run(async()=>{resetCorrection();await sales();await history();message('Magasin sélectionné : '+$('store').selectedOptions[0].text);});
$('saleSearch').onsubmit=e=>{e.preventDefault();run(sales);};
$('customerSearch').onsubmit=e=>{e.preventDefault();$('review').hidden=true;pending=null;$('correction').hidden=false;run(customers);};
$('refreshHistory').onclick=()=>run(history);
$('correction').onsubmit=e=>{e.preventDefault();if(!selected)return;pending={store:$('store').value,saleId:selected.id,body:{operationId:crypto.randomUUID(),customerId:$('customer').value,expectedCustomerId:selected.customerId,expectedModifiedAt:selected.modifiedAt,expectedDebt:selected.debtToTransfer,reason:$('reason').value.trim()}};$('reviewText').textContent=`Magasin ${$('store').selectedOptions[0].text} — ticket ${selected.invoiceNumber} : remplacer ${selected.customerName} par ${$('customer').selectedOptions[0].text}. Motif : ${pending.body.reason}. Dette à transférer : ${money(selected.debtToTransfer)}. Le serveur vérifiera que le dossier permet ce transfert.`;$('correction').hidden=true;$('review').hidden=false;};
$('cancel').onclick=()=>{pending=null;$('review').hidden=true;$('correction').hidden=false;};
$('confirm').onclick=()=>run(async()=>{if(!pending)return;const result=await api(`/api/support/${pending.store}/sales/${pending.saleId}/customer`,pending.body);resetCorrection();message(`Correction enregistrée. Dette transférée : ${money(result.transferredDebt)}. Elle sera reçue par les postes lors de leur prochaine synchronisation.`);await sales();await history();});
