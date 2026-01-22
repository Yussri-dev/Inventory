window.printReceipt = function () {
    try {
        const receiptContent = document.getElementById('receipt');
        if (!receiptContent) {
            console.error('Receipt element not found');
            return;
        }

        // Create a print-specific div
        const printDiv = document.createElement('div');
        printDiv.id = 'print-area';
        printDiv.innerHTML = `
            <style>
                @media print {
                    body * { visibility: hidden; }
                    #print-area, #print-area * { visibility: visible; }
                    #print-area {
                        position: absolute;
                        left: 0;
                        top: 0;
                        width: 100%;
                    }
                }
                #print-area {
                    font-family: 'Courier New', monospace;
                    font-size: 12px;
                    max-width: 300px;
                    margin: 0 auto;
                }
                #print-area h3 {
                    text-align: center;
                    margin: 10px 0;
                }
                #print-area hr {
                    border: none;
                    border-top: 1px dashed #000;
                    margin: 10px 0;
                }
            </style>
            ${receiptContent.innerHTML}
        `;

        // Append to body temporarily
        document.body.appendChild(printDiv);

        // Trigger print
        window.print();

        // Remove after a delay
        setTimeout(() => {
            document.body.removeChild(printDiv);
        }, 1000);

    } catch (error) {
        console.error('Print error:', error);
        alert('Failed to print receipt: ' + error.message);
    }
};