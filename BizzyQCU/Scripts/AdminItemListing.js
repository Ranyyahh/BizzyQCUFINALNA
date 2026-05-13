// ============================================================
// AdminItemListing.js - Dynamic Products Listing
// ============================================================

const urlParamsItem = new URLSearchParams(window.location.search);
const enterpriseIdItem = urlParamsItem.get('enterpriseId');
let allProductsItem = [];
let filteredProductsItem = [];
let selectedProductIdItem = null;
let approveUiOnOk = null;
let reportsChartItem = null;

let currentPageItem = 1;
let totalPagesItem = 1;
let pageSizeItem = 6;

function getColumnsPerRowItem() {
    if (window.innerWidth <= 580) return 1;
    if (window.innerWidth <= 960) return 2;
    return 3;
}

function computePageSizeItem() {
    pageSizeItem = getColumnsPerRowItem() * 2;
}

document.addEventListener('DOMContentLoaded', function () {
    computePageSizeItem();

    if (enterpriseIdItem && enterpriseIdItem !== 'null' && enterpriseIdItem !== 'undefined') {
        loadEnterpriseDetailsItem();
        loadProductsItem();
    } else {
        document.getElementById('profileName').textContent = 'No Enterprise Selected';
        document.getElementById('productsGrid').innerHTML = '<div class="empty-state">No enterprise selected</div>';
    }

    setupEventListenersItem();
});

function loadEnterpriseDetailsItem() {
    fetch(`/AdminPanel/GetEnterpriseDetails?enterpriseId=${enterpriseIdItem}`)
        .then(response => {
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
            return response.json();
        })
        .then(data => {
            if (data && data.EnterpriseId) populateProfileItem(data);
            else if (data.message) document.getElementById('profileName').textContent = 'Enterprise not found';
        })
        .catch(() => {
            document.getElementById('profileName').textContent = 'Error loading enterprise';
        });
}

function loadProductsItem() {
    fetch(`/AdminPanel/GetProductsForListing?enterpriseId=${enterpriseIdItem}`)
        .then(response => response.json())
        .then(data => {
            allProductsItem = data || [];
            currentPageItem = 1;
            applyFiltersAndRenderItem();
        })
        .catch(() => {
            document.getElementById('productsGrid').innerHTML = '<div class="empty-state">Error loading products</div>';
            updatePaginationUiItem(0);
        });
}

function populateProfileItem(enterprise) {
    const nameEl = document.getElementById('profileName');
    if (nameEl) nameEl.textContent = enterprise.StoreName || 'Enterprise Name';

    const idEl = document.getElementById('profileId');
    if (idEl) idEl.textContent = enterprise.EnterpriseId || 'N/A';

    const emailEl = document.getElementById('profileEmail');
    if (emailEl) emailEl.textContent = enterprise.Email || 'No email';

    const statusEl = document.getElementById('profileStatus');
    const status = enterprise.Status || 'pending';
    if (statusEl) {
        statusEl.textContent = status.charAt(0).toUpperCase() + status.slice(1);
        statusEl.className = 'info-value status-' + status.toLowerCase();
    }

    const rating = enterprise.RatingAvg || 0;
    const starsEl = document.getElementById('profileStars');
    if (starsEl) {
        starsEl.innerHTML = '';
        const roundedRating = Math.round(rating);
        for (let i = 1; i <= 5; i++) {
            const starSpan = document.createElement('span');
            starSpan.className = 'star' + (i <= roundedRating ? ' filled' : '');
            starSpan.textContent = '★';
            starsEl.appendChild(starSpan);
        }
    }

    const firstLetter = enterprise.StoreName ? enterprise.StoreName.charAt(0).toUpperCase() : 'E';
    const colors = ['#4A6CF7', '#7B3FE4', '#d4a017', '#2e4dbf', '#e03131'];
    const colorIndex = (enterprise.EnterpriseId || 1) % colors.length;
    const bgColor = colors[colorIndex];

    const avatarDiv = document.getElementById('profileAvatar');
    if (avatarDiv) {
        avatarDiv.innerHTML = `<div style="width: 72px; height: 72px; border-radius: 50%; background: ${bgColor}; display: flex; align-items: center; justify-content: center;"><span style="color: white; font-size: 32px; font-weight: 600;">${escapeHtmlItem(firstLetter)}</span></div>`;
    }
}

function isActiveProductItem(product) {
    const normalizedStatus = (product.Status || '').toLowerCase();
    const approvalState = Number(product.ApprovalState ?? 0);
    return approvalState === 1 && (normalizedStatus === 'active' || normalizedStatus === 'approved');
}

function isInactiveProductItem(product) {
    return (product.Status || '').toLowerCase() === 'inactive';
}

function applyFiltersAndRenderItem() {
    const query = (document.getElementById('productSearch')?.value || '').toLowerCase().trim();
    const status = document.getElementById('statusFilter')?.value || 'all';

    filteredProductsItem = (allProductsItem || []).filter(p => {
        const matchesSearch = (p.ProductName || '').toLowerCase().includes(query);
        if (!matchesSearch) return false;
        if (status === 'active') return isActiveProductItem(p);
        if (status === 'inactive') return isInactiveProductItem(p);
        return true;
    });

    totalPagesItem = Math.max(1, Math.ceil(filteredProductsItem.length / pageSizeItem));
    if (currentPageItem > totalPagesItem) currentPageItem = totalPagesItem;
    if (currentPageItem < 1) currentPageItem = 1;

    renderCurrentPageItem();
    updatePaginationUiItem(filteredProductsItem.length);
}

function renderCurrentPageItem() {
    const start = (currentPageItem - 1) * pageSizeItem;
    const pageProducts = filteredProductsItem.slice(start, start + pageSizeItem);
    renderProductsItem(pageProducts);
}

function renderProductsItem(products) {
    const grid = document.getElementById('productsGrid');
    if (!grid) return;

    if (!products || products.length === 0) {
        grid.innerHTML = '<div class="empty-state">No products found</div>';
        selectedProductIdItem = null;
        return;
    }

    grid.innerHTML = '';

    products.forEach(product => {
        const card = document.createElement('div');
        card.className = 'product-card';
        card.dataset.id = product.ProductId;
        card.dataset.name = (product.ProductName || '').toLowerCase();
        card.addEventListener('click', () => selectProductItem(product.ProductId));

        const normalizedStatus = (product.Status || '').toLowerCase();
        const approvalState = Number(product.ApprovalState ?? 0);
        let statusBadge = '<span class="status-badge unknown">Unknown</span>';
        if (approvalState === -1) statusBadge = '<span class="status-badge rejected">Rejected</span>';
        else if (approvalState === 0) statusBadge = '<span class="status-badge pending">Pending Approval</span>';
        else if (approvalState === 1 && (normalizedStatus === 'active' || normalizedStatus === 'approved')) statusBadge = '<span class="status-badge active">Active</span>';
        else if (normalizedStatus === 'inactive') statusBadge = '<span class="status-badge inactive">Inactive</span>';

        const productImageMarkup = product.ProductImage
            ? `<img class="product-img" src="${product.ProductImage}" alt="${escapeHtmlItem(product.ProductName || 'Product')}" />`
            : `<div class="product-placeholder">${escapeHtmlItem((product.ProductName || 'P').charAt(0))}</div>`;

        card.innerHTML = `
            <div class="product-image-wrap">${productImageMarkup}</div>
            <div class="product-info-wrap">
                <div class="product-title">${escapeHtmlItem(product.ProductName || 'Unknown')}</div>
                <div class="product-detail-line"><span class="detail-label">Price:</span> ₱${parseFloat(product.Price || 0).toFixed(2)}</div>
                <div class="product-detail-line"><span class="detail-label">Description:</span> ${escapeHtmlItem(product.Description || 'No description')}</div>
                ${statusBadge}
            </div>
        `;
        grid.appendChild(card);
    });

    const selectedStillVisible = products.some(p => parseInt(p.ProductId, 10) === parseInt(selectedProductIdItem, 10));
    if (!selectedStillVisible) selectedProductIdItem = products[0].ProductId;
    selectProductItem(selectedProductIdItem);
}

function updatePaginationUiItem(totalCount) {
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    const pageInfo = document.getElementById('pageInfo');

    const hasData = totalCount > 0;
    const effectivePages = hasData ? totalPagesItem : 1;

    if (pageInfo) pageInfo.textContent = hasData ? `Page ${currentPageItem} of ${effectivePages}` : 'Page 0 of 0';
    if (prevBtn) prevBtn.disabled = !hasData || currentPageItem <= 1;
    if (nextBtn) nextBtn.disabled = !hasData || currentPageItem >= effectivePages;
}

function selectProductItem(productId) {
    selectedProductIdItem = productId;
    const cards = document.querySelectorAll('.product-card');
    cards.forEach(card => card.classList.toggle('selected', parseInt(card.dataset.id, 10) === parseInt(productId, 10)));
}

function setupEventListenersItem() {
    document.getElementById('productSearch')?.addEventListener('input', function () {
        currentPageItem = 1;
        applyFiltersAndRenderItem();
    });

    document.getElementById('statusFilter')?.addEventListener('change', function () {
        currentPageItem = 1;
        applyFiltersAndRenderItem();
    });

    document.getElementById('prevPageBtn')?.addEventListener('click', function () {
        if (currentPageItem > 1) {
            currentPageItem--;
            renderCurrentPageItem();
            updatePaginationUiItem(filteredProductsItem.length);
        }
    });

    document.getElementById('nextPageBtn')?.addEventListener('click', function () {
        if (currentPageItem < totalPagesItem) {
            currentPageItem++;
            renderCurrentPageItem();
            updatePaginationUiItem(filteredProductsItem.length);
        }
    });

    window.addEventListener('resize', function () {
        const oldSize = pageSizeItem;
        computePageSizeItem();
        if (oldSize !== pageSizeItem) {
            currentPageItem = 1;
            applyFiltersAndRenderItem();
        }
    });

    const approveBtn = document.getElementById('approveItemBtn');
    if (approveBtn) {
        approveBtn.addEventListener('click', () => {
            const effectiveProductId = selectedProductIdItem || getFirstVisibleProductId();
            if (!effectiveProductId) return showToastItem('Please select a product first', 'error');

            const selected = getProductById(effectiveProductId);
            if (!selected) return showToastItem('Product not found.', 'error');

            const normalizedStatus = (selected.Status || '').toLowerCase();
            const approvalState = Number(selected.ApprovalState ?? 0);
            if (approvalState === 1 && (normalizedStatus === 'active' || normalizedStatus === 'approved')) {
                showApproveUi('Already Approved', `"${selected.ProductName || 'This product'}" is already approved.`, false);
                return;
            }

            showApproveUi('Confirm Approval', `Are you sure you want to approve "${selected.ProductName || 'this product'}"?`, true, () => approveProductItem(effectiveProductId));
        });
    }

    document.getElementById('removeItemBtn')?.addEventListener('click', () => {
        const effectiveProductId = selectedProductIdItem || getFirstVisibleProductId();
        if (!effectiveProductId) return showToastItem('Please select a product first', 'error');
        if (confirm('Are you sure you want to reject this product?')) removeProductItem(effectiveProductId);
    });

    const deleteBtn = document.getElementById('deleteBtn');
    const modalBackdrop = document.getElementById('deleteModal');
    const cancelDelete = document.getElementById('cancelDelete');
    const confirmDeleteBtn = document.getElementById('confirmDelete');

    deleteBtn?.addEventListener('click', () => {
        const name = document.getElementById('profileName').textContent;
        const modalName = document.getElementById('modalEnterpriseName');
        if (modalName) modalName.textContent = name;
        modalBackdrop?.classList.add('active');
    });

    cancelDelete?.addEventListener('click', () => modalBackdrop?.classList.remove('active'));

    modalBackdrop?.addEventListener('click', (e) => {
        if (e.target === modalBackdrop) modalBackdrop.classList.remove('active');
    });

    confirmDeleteBtn?.addEventListener('click', () => {
        fetch('/AdminPanel/DeleteEnterprise', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `enterpriseId=${enterpriseIdItem}`
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    showToastItem('Enterprise deleted successfully', 'success');
                    setTimeout(() => { window.location.href = '/AdminPanel/AdminLandingEntrep'; }, 1500);
                } else {
                    showToastItem('Failed to delete enterprise: ' + (data.message || 'Unknown error'), 'error');
                    modalBackdrop?.classList.remove('active');
                }
            })
            .catch(() => {
                showToastItem('An error occurred', 'error');
                modalBackdrop?.classList.remove('active');
            });
    });
}

function approveProductItem(productId) {
    const formData = new URLSearchParams();
    formData.append('productId', productId);

    fetch('/AdminPanel/ApproveProduct', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToastItem('Product approved successfully', 'success');
                loadProductsItem();
                selectedProductIdItem = null;
            } else {
                showToastItem('Failed to approve product: ' + (data.message || 'Unknown error'), 'error');
            }
        })
        .catch(() => showToastItem('An error occurred', 'error'));
}

function removeProductItem(productId) {
    const formData = new URLSearchParams();
    formData.append('productId', productId);

    fetch('/AdminPanel/RemoveProduct', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showToastItem('Product rejected successfully', 'success');
                loadProductsItem();
                selectedProductIdItem = null;
            } else {
                showToastItem('Failed to reject product: ' + (data.message || 'Unknown error'), 'error');
            }
        })
        .catch(() => showToastItem('An error occurred', 'error'));
}

function showToastItem(message, type) {
    const toast = document.getElementById('toast');
    if (!toast) return;
    toast.textContent = message;
    toast.className = 'toast ' + type;
    toast.style.display = 'block';
    setTimeout(() => { toast.style.display = 'none'; }, 3000);
}

function escapeHtmlItem(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

const viewReportsBtn = document.getElementById('viewReportsBtn');
if (viewReportsBtn) {
    viewReportsBtn.addEventListener('click', () => {
        openWeeklyReportsItem();
    });
}

function getProductById(productId) {
    for (let i = 0; i < allProductsItem.length; i++) {
        if (parseInt(allProductsItem[i].ProductId, 10) === parseInt(productId, 10)) return allProductsItem[i];
    }
    return null;
}

function showApproveUi(title, message, showCancel, onOk) {
    const modal = document.getElementById('approveUiModal');
    const titleEl = document.getElementById('approveUiTitle');
    const messageEl = document.getElementById('approveUiMessage');
    const cancelBtn = document.getElementById('approveUiCancelBtn');
    const okBtn = document.getElementById('approveUiOkBtn');
    if (!modal || !titleEl || !messageEl || !cancelBtn || !okBtn) return;

    titleEl.textContent = title || 'Notice';
    messageEl.textContent = message || '';
    cancelBtn.style.display = showCancel ? 'inline-block' : 'none';
    okBtn.textContent = showCancel ? 'Yes, Approve' : 'OK';
    approveUiOnOk = typeof onOk === 'function' ? onOk : null;

    modal.classList.add('show');
    modal.setAttribute('aria-hidden', 'false');
}

function closeApproveUi() {
    const modal = document.getElementById('approveUiModal');
    if (!modal) return;
    modal.classList.remove('show');
    modal.setAttribute('aria-hidden', 'true');
    approveUiOnOk = null;
}

document.getElementById('approveUiCancelBtn')?.addEventListener('click', closeApproveUi);

document.getElementById('approveUiOkBtn')?.addEventListener('click', () => {
    const action = approveUiOnOk;
    closeApproveUi();
    if (typeof action === 'function') action();
});

document.getElementById('approveUiModal')?.addEventListener('click', (e) => {
    if (e.target === document.getElementById('approveUiModal')) closeApproveUi();
});

function getFirstVisibleProductId() {
    const cards = document.querySelectorAll('.product-card');
    for (let i = 0; i < cards.length; i++) {
        const id = parseInt(cards[i].dataset.id, 10);
        if (!isNaN(id)) return id;
    }
    return null;
}

function openWeeklyReportsItem() {
    const modal = document.getElementById('reportsModal');
    const subtitle = document.getElementById('reportsSubtitle');
    const totalEl = document.getElementById('reportsTotal');
    if (!modal) return;

    modal.classList.add('show');
    modal.setAttribute('aria-hidden', 'false');
    if (subtitle) subtitle.textContent = 'Loading sales data...';
    if (totalEl) totalEl.textContent = 'Total: ₱ 0';

    fetch(`/AdminPanel/GetSalesData?enterpriseId=${enterpriseIdItem}&days=7`)
        .then(response => response.json())
        .then(data => {
            const chartRows = normalizeWeeklySalesRows(data || []);
            const labels = chartRows.map(row => row.Label);
            const values = chartRows.map(row => Number(row.Value || 0));
            const total = values.reduce((sum, value) => sum + value, 0);
            const enterpriseName = document.getElementById('profileName')?.textContent || 'Enterprise';

            if (subtitle) subtitle.textContent = `${enterpriseName} sales for the last 7 days`;
            if (totalEl) totalEl.textContent = `Total: ₱ ${total.toLocaleString()}`;
            renderReportsChartItem(labels, values);
        })
        .catch(() => {
            if (subtitle) subtitle.textContent = 'Unable to load weekly sales.';
            renderReportsChartItem(['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'], [0, 0, 0, 0, 0, 0, 0]);
        });
}

function normalizeWeeklySalesRows(rows) {
    const formatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: '2-digit' });
    const today = new Date();
    const result = [];

    for (let i = 6; i >= 0; i--) {
        const day = new Date(today);
        day.setDate(today.getDate() - i);
        const label = formatter.format(day);
        const match = rows.find(row => row.Label === label);
        result.push({ Label: label, Value: match ? match.Value : 0 });
    }

    return result;
}

function renderReportsChartItem(labels, values) {
    const canvas = document.getElementById('reportsChart');
    if (!canvas || typeof Chart === 'undefined') return;

    if (reportsChartItem) reportsChartItem.destroy();
    reportsChartItem = new Chart(canvas, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Weekly Sales',
                data: values,
                borderColor: '#d4a017',
                backgroundColor: 'rgba(212, 160, 23, 0.14)',
                borderWidth: 3,
                pointRadius: 5,
                pointBackgroundColor: '#1a1f4e',
                pointBorderColor: '#d4a017',
                fill: true,
                tension: 0.35
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: { callbacks: { label: context => `₱ ${Number(context.raw || 0).toLocaleString()}` } }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: { callback: value => `₱${Number(value || 0).toLocaleString()}` }
                }
            }
        }
    });
}

function closeWeeklyReportsItem() {
    const modal = document.getElementById('reportsModal');
    if (!modal) return;
    modal.classList.remove('show');
    modal.setAttribute('aria-hidden', 'true');
}

document.getElementById('closeReportsBtn')?.addEventListener('click', closeWeeklyReportsItem);

document.getElementById('reportsModal')?.addEventListener('click', (e) => {
    if (e.target === document.getElementById('reportsModal')) closeWeeklyReportsItem();
});