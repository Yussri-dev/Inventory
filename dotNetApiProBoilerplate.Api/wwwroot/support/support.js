'use strict';

const $ = id => document.getElementById(id);

let token = null;
let currentRole = null;

let selected = null;
let pending = null;
let busy = false;

let selectedCatalogApproval = null;

const money = value =>
    new Intl.NumberFormat('fr-FR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(value);


// ======================================================
// MESSAGE
// ======================================================

function message(text, error = false) {
    $('message').textContent = text;
    $('message').classList.toggle('error', error);
}


// ======================================================
// RESET CORRECTION
// ======================================================

function resetCorrection() {
    selected = null;
    pending = null;

    $('correctionPanel').hidden = true;
    $('review').hidden = true;
    $('correction').hidden = false;
}


// ======================================================
// LOGOUT
// ======================================================

function logout() {

    token = null;
    currentRole = null;

    resetCorrection();

    $('workspace').hidden = true;
    $('logout').hidden = true;
    $('loginPanel').hidden = false;

    $('sales').replaceChildren();
    $('history').replaceChildren();
    $('catalogApprovals').replaceChildren();
}


// ======================================================
// API
// ======================================================

async function api(path, body) {

    const response = await fetch(path, {
        method: body === undefined ? 'GET' : 'POST',

        cache: 'no-store',

        headers: {
            ...(token
                ? { Authorization: `Bearer ${token}` }
                : {}),

            ...(body === undefined
                ? {}
                : { 'Content-Type': 'application/json' })
        },

        ...(body === undefined
            ? {}
            : { body: JSON.stringify(body) })
    });

    const data =
        await response
            .json()
            .catch(() => ({}));

    if (!response.ok) {

        if (response.status === 401)
            logout();

        throw new Error(
            data.detail ||
            data.title ||
            (
                response.status === 403
                    ? 'Accès réservé aux administrateurs autorisés.'
                    : `Erreur ${response.status}. Reconnectez-vous si votre session a expiré.`
            )
        );
    }

    return data;
}


// ======================================================
// TENANT SCOPED URL
// ======================================================

function scoped(path) {

    return `/api/support/${encodeURIComponent(
        $('store').value
    )}/${path}`;
}


// ======================================================
// TABLE ROW HELPER
// ======================================================

function row(parent, values) {

    const tr =
        document.createElement('tr');

    for (const value of values) {

        const td =
            document.createElement('td');

        if (value instanceof Node)
            td.append(value);
        else
            td.textContent = value ?? '';

        tr.append(td);
    }

    parent.append(tr);
}


// ======================================================
// SALES
// ======================================================

async function sales() {

    resetCorrection();

    const list =
        await api(
            scoped(
                `sales?search=${encodeURIComponent(
                    $('saleQuery').value
                )}`
            )
        );

    $('sales').replaceChildren();

    for (const sale of list) {

        const button =
            document.createElement('button');

        button.textContent =
            'Corriger le client';

        button.onclick = () =>
            run(async () => {

                resetCorrection();

                selected = sale;

                $('selectedSale').textContent =
                    `Ticket ${sale.invoiceNumber} · ` +
                    `${sale.customerName} · ` +
                    `${money(sale.totalAmount)}`;

                $('reason').value = '';
                $('customerQuery').value = '';

                $('correctionPanel').hidden = false;

                await customers();

                $('correctionPanel')
                    .scrollIntoView({
                        behavior: 'smooth'
                    });
            });

        row(
            $('sales'),
            [
                sale.invoiceNumber,
                new Date(
                    sale.saleDate
                ).toLocaleString('fr-FR'),
                sale.customerName,
                money(sale.totalAmount),
                button
            ]
        );
    }

    if (!list.length) {
        row(
            $('sales'),
            ['Aucune vente correspondante.']
        );
    }
}


// ======================================================
// CATALOG APPROVALS
// SUPERADMIN ONLY
// ======================================================

async function catalogApprovals() {

    if (currentRole !== 'SuperAdmin')
        return;

    const list =
        await api(
            '/api/support/catalog-approvals'
        );

    $('catalogApprovals')
        .replaceChildren();

    for (const item of list) {

        // -----------------------------
        // APPROVE BUTTON
        // -----------------------------

        const approve =
            document.createElement('button');

        approve.textContent =
            'Approuver';

        approve.onclick = async () => {
            selectedCatalogApproval = item;

            $('catalogApprovalProduct').textContent =
                `${item.name} · ${item.barcode ?? 'sans barcode'} · ${item.tenantName ?? item.tenantId}`;

            $('catalogCategoryId').value = '';
            $('catalogManufacturer').value = item.brand ?? '';
            $('catalogSellingMode').value = 'Unit';
            $('catalogUnitOfMeasure').value = item.unit || 'pcs';

            await loadCatalogCategories();

            $('catalogApprovalPanel').hidden = false;

            $('catalogApprovalPanel')
                .scrollIntoView({
                    behavior: 'smooth'
                });
        };


        // -----------------------------
        // REJECT BUTTON
        // -----------------------------

        const reject =
            document.createElement('button');

        reject.textContent =
            'Refuser';

        reject.classList.add('secondary');

        reject.onclick = () =>
            run(async () => {

                await api(
                    `/api/support/catalog-approvals/${item.id}/reject`,
                    {}
                );

                message(
                    `Produit "${item.name}" refusé.`
                );

                await catalogApprovals();
            });


        // -----------------------------
        // ROW
        // -----------------------------

        row(
            $('catalogApprovals'),
            [
                item.tenantName ??
                item.tenantId,

                item.name,

                item.barcode ?? '',

                item.brand ?? '',

                item.category ?? '',

                item.unit ?? '',

                approve,

                reject
            ]
        );
    }


    // -----------------------------
    // EMPTY LIST
    // -----------------------------

    if (!list.length) {

        row(
            $('catalogApprovals'),
            [
                'Aucune demande en attente.'
            ]
        );
    }
}


// ======================================================
// CUSTOMERS
// ======================================================

async function customers() {

    const list =
        await api(
            scoped(
                `customers?search=${encodeURIComponent(
                    $('customerQuery').value
                )}`
            )
        );

    $('customer').replaceChildren(
        new Option(
            'Sélectionner le bon client',
            ''
        )
    );

    for (const customer of list) {

        $('customer').add(
            new Option(
                `${customer.name}` +
                `${customer.phone
                    ? ' · ' + customer.phone
                    : ''}` +
                ` · Solde ${money(
                    customer.currentBalance
                )}`,
                customer.id
            )
        );
    }
}


// ======================================================
// HISTORY
// ======================================================

async function history() {

    const list =
        await api(
            scoped('corrections')
        );

    $('history')
        .replaceChildren();

    for (const item of list) {

        row(
            $('history'),
            [
                new Date(
                    item.createdAt
                ).toLocaleString('fr-FR'),

                item.invoiceNumber,

                item.reason,

                money(
                    item.transferredDebt
                ),

                item.id
            ]
        );
    }

    if (!list.length) {

        row(
            $('history'),
            [
                'Aucune correction enregistrée.'
            ]
        );
    }
}

async function loadCatalogCategories() {

    if (currentRole !== 'SuperAdmin')
        return;

    const categories =
        await api(
            '/api/support/catalog-categories'
        );

    const select =
        $('catalogCategoryId');

    select.replaceChildren(
        new Option(
            '-- Sélectionner une catégorie --',
            ''
        )
    );

    for (const category of categories) {

        select.add(
            new Option(
                category.name,
                category.id
            )
        );
    }
}


// ======================================================
// BUSY HANDLER
// ======================================================

async function run(action) {

    if (busy)
        return;

    busy = true;

    document
        .querySelectorAll('button')
        .forEach(
            button =>
                button.disabled = true
        );

    try {

        await action();

    }
    catch (error) {

        message(
            error.message,
            true
        );

    }
    finally {

        busy = false;

        document
            .querySelectorAll('button')
            .forEach(
                button =>
                    button.disabled = false
            );
    }
}


// ======================================================
// LOGIN
// ======================================================

$('login').onsubmit = event => {

    event.preventDefault();

    run(async () => {

        const auth =
            await api(
                '/api/auth/login',
                {
                    email:
                        $('email').value,

                    password:
                        $('password').value
                }
            );

        $('password').value = '';

        if (
            ![
                'Admin',
                'SuperAdmin'
            ].includes(auth.role)
        ) {
            throw new Error(
                'Ce compte ne dispose pas des droits administrateur.'
            );
        }

        token =
            auth.accessToken;

        currentRole =
            auth.role;


        // ---------------------------------
        // STORES
        // ---------------------------------

        const stores =
            await api(
                '/api/support/stores'
            );

        $('store')
            .replaceChildren();

        for (const store of stores) {

            $('store').add(
                new Option(
                    store.name,
                    store.id
                )
            );
        }


        // ---------------------------------
        // SHOW WORKSPACE
        // ---------------------------------

        $('loginPanel').hidden =
            true;

        $('workspace').hidden =
            false;

        $('logout').hidden =
            false;


        // ---------------------------------
        // CATALOG SECTION
        // SuperAdmin only
        // ---------------------------------

        const catalogSection =
            $('catalogApprovals')
                .closest('section');

        if (catalogSection) {
            catalogSection.hidden =
                currentRole !==
                'SuperAdmin';
        }


        // ---------------------------------
        // STORE DATA
        // ---------------------------------

        if (stores.length) {

            await sales();

            await history();

        }


        // ---------------------------------
        // GLOBAL CATALOG APPROVALS
        // ---------------------------------

        if (
            currentRole ===
            'SuperAdmin'
        ) {
            await catalogApprovals();
        }


        // ---------------------------------
        // MESSAGE
        // ---------------------------------

        if (stores.length) {

            message(
                currentRole ===
                    'SuperAdmin'
                    ? 'Support chargé. Les demandes catalogue sont également disponibles.'
                    : 'Sélectionnez une vente dans le magasin concerné.'
            );

        }
        else {

            message(
                currentRole ===
                    'SuperAdmin'
                    ? 'Aucun magasin accessible. Les demandes catalogue restent disponibles.'
                    : 'Aucun magasin accessible.'
            );
        }
    });
};


// ======================================================
// LOGOUT BUTTON
// ======================================================

$('logout').onclick = () => {

    logout();

    message(
        'Vous êtes déconnecté.'
    );
};


// ======================================================
// STORE CHANGE
// ======================================================

$('store').onchange = () =>
    run(async () => {

        resetCorrection();

        await sales();

        await history();

        message(
            'Magasin sélectionné : ' +
            $('store')
                .selectedOptions[0]
                .text
        );
    });


// ======================================================
// SALE SEARCH
// ======================================================

$('saleSearch').onsubmit =
    event => {

        event.preventDefault();

        run(sales);
    };


// ======================================================
// CUSTOMER SEARCH
// ======================================================

$('customerSearch').onsubmit =
    event => {

        event.preventDefault();

        $('review').hidden =
            true;

        pending =
            null;

        $('correction').hidden =
            false;

        run(customers);
    };


// ======================================================
// REFRESH HISTORY
// ======================================================

$('refreshHistory').onclick =
    () => run(history);


// ======================================================
// REFRESH CATALOG APPROVALS
// ======================================================

$('refreshCatalogApprovals').onclick =
    () => {

        if (
            currentRole !==
            'SuperAdmin'
        ) {
            return;
        }

        run(
            catalogApprovals
        );
    };


// ======================================================
// PREPARE CUSTOMER CORRECTION
// ======================================================

$('correction').onsubmit =
    event => {

        event.preventDefault();

        if (!selected)
            return;

        pending = {

            store:
                $('store').value,

            saleId:
                selected.id,

            body: {

                operationId:
                    crypto.randomUUID(),

                customerId:
                    $('customer').value,

                expectedCustomerId:
                    selected.customerId,

                expectedModifiedAt:
                    selected.modifiedAt,

                expectedDebt:
                    selected.debtToTransfer,

                reason:
                    $('reason')
                        .value
                        .trim()
            }
        };


        $('reviewText').textContent =
            `Magasin ${$('store').selectedOptions[0].text} — ` +
            `ticket ${selected.invoiceNumber} : ` +
            `remplacer ${selected.customerName} par ` +
            `${$('customer').selectedOptions[0].text}. ` +
            `Motif : ${pending.body.reason}. ` +
            `Dette à transférer : ${money(selected.debtToTransfer)}. ` +
            `Le serveur vérifiera que le dossier permet ce transfert.`;

        $('correction').hidden =
            true;

        $('review').hidden =
            false;
    };


// ======================================================
// CANCEL CUSTOMER CORRECTION
// ======================================================

$('cancel').onclick =
    () => {

        pending =
            null;

        $('review').hidden =
            true;

        $('correction').hidden =
            false;
    };


// ======================================================
// CONFIRM CUSTOMER CORRECTION
// ======================================================

$('confirm').onclick =
    () =>
        run(async () => {

            if (!pending)
                return;

            const result =
                await api(
                    `/api/support/${pending.store}/sales/${pending.saleId}/customer`,
                    pending.body
                );

            resetCorrection();

            message(
                `Correction enregistrée. ` +
                `Dette transférée : ` +
                `${money(result.transferredDebt)}. ` +
                `Elle sera reçue par les postes lors de leur prochaine synchronisation.`
            );

            await sales();

            await history();
        });


// ======================================================
// CONFIRM CATALOG APPROVAL
// ======================================================

$('catalogApprovalForm').onsubmit = event => {

    event.preventDefault();

    if (!selectedCatalogApproval)
        return;

    run(async () => {

        const body = {
            categoryId:
                $('catalogCategoryId').value.trim(),

            manufacturer:
                $('catalogManufacturer').value.trim() || null,

            sellingMode:
                $('catalogSellingMode').value,

            unitOfMeasure:
                $('catalogUnitOfMeasure').value
        };

        await api(
            `/api/support/catalog-approvals/${selectedCatalogApproval.id}/approve`,
            body
        );

        message(
            `Produit "${selectedCatalogApproval.name}" approuvé et ajouté au catalogue.`
        );

        selectedCatalogApproval = null;

        $('catalogApprovalPanel').hidden = true;

        $('catalogApprovalForm').reset();

        await catalogApprovals();
    });
};

// ======================================================
// CANCEL CATALOG APPROVAL
// ======================================================

$('cancelCatalogApproval').onclick = () => {

    selectedCatalogApproval = null;

    $('catalogApprovalPanel').hidden = true;

    $('catalogApprovalForm').reset();
};