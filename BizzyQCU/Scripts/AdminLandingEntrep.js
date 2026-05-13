
const searchInput = document.getElementById('searchInput');
const grid = document.getElementById('enterprisesGrid');
const countBadge = document.getElementById('countBadge');
const prevPageBtn = document.getElementById('prevPageBtn');
const nextPageBtn = document.getElementById('nextPageBtn');
const pageStatus = document.getElementById('pageStatus');
const paginationActions = document.getElementById('paginationActions');
const pageSize = 6;
let allEnterprises = [];
let filteredEnterprises = [];
let currentPage = 1;

if (!grid || !countBadge) {
    console.error("Missing required DOM elements (grid or countBadge).");
}

function loadEnterprises() {
    fetch('/AdminPanel/GetAllApprovedEnterprises')
        .then(response => response.json())
        .then(data => {
            console.log('Enterprises data:', data);

            if (!data || data.length === 0) {
                showEmptyState();
                return;
            }

            allEnterprises = data;
            filteredEnterprises = data;
            currentPage = 1;
            renderCards();
        })
        .catch(error => {
            console.error('Error loading enterprises:', error);
            showEmptyState();
            grid.innerHTML = `
                <div class="empty-state">
                    <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                        <circle cx="12" cy="12" r="10"></circle>
                        <line x1="12" y1="8" x2="12" y2="12"></line>
                        <line x1="12" y1="16" x2="12.01" y2="16"></line>
                    </svg>
                    <p>Error loading enterprises. Please try again.</p>
                </div>
            `;
            updateCount(0);
        });
}

// ── RENDER SINGLE CARD ──
function renderCard(enterprise) {
    const rating = enterprise.RatingAvg || 0;
    const fullStars = Math.floor(rating);
    const hasHalfStar = (rating - fullStars) >= 0.5;

    let starsHtml = '';
    for (let i = 1; i <= 5; i++) {
        if (i <= fullStars) {
            starsHtml += '<span class="star filled">★</span>';
        } else if (i === fullStars + 1 && hasHalfStar) {
            starsHtml += '<span class="star half">★</span>';
        } else {
            starsHtml += '<span class="star">★</span>';
        }
    }

    // Avatar with first letter fallback
    const firstLetter = enterprise.StoreName ? enterprise.StoreName.charAt(0).toUpperCase() : 'E';
    const colors = ['#4A6CF7', '#7B3FE4', '#d4a017', '#2e4dbf', '#e03131'];
    const colorIndex = (enterprise.EnterpriseId || 1) % colors.length;
    const bgColor = colors[colorIndex];

    const card = document.createElement('div');
    card.className = 'enterprise-card';
    card.dataset.name = (enterprise.StoreName || '').toLowerCase();
    card.dataset.id = enterprise.EnterpriseId;

    card.innerHTML = `
        <div class="card-avatar" style="background: ${bgColor}; display: flex; align-items: center; justify-content: center;">
            <span style="color: white; font-size: 32px; font-weight: 600;">${escapeHtml(firstLetter)}</span>
        </div>
        <div class="card-name">${escapeHtml(enterprise.StoreName)}</div>
        <div class="card-id">${escapeHtml(enterprise.Username || 'No username')}</div>
        <div class="card-stars">${starsHtml}</div>
        <a href="/AdminPanel/AdminEditEntrep?enterpriseId=${enterprise.EnterpriseId}" class="view-btn">
            View Account
        </a>
    `;

    return card;
}

// ── RENDER ALL ──
function renderCards() {
    grid.innerHTML = '';

    if (!filteredEnterprises || filteredEnterprises.length === 0) {
        showEmptyState();
        return;
    }

    const totalPages = Math.max(1, Math.ceil(filteredEnterprises.length / pageSize));
    if (currentPage > totalPages) currentPage = totalPages;
    if (currentPage < 1) currentPage = 1;

    const start = (currentPage - 1) * pageSize;
    const pageItems = filteredEnterprises.slice(start, start + pageSize);

    pageItems.forEach(enterprise => {
        grid.appendChild(renderCard(enterprise));
    });

    updateCount(filteredEnterprises.length);
    updatePagination(totalPages);
}

function updateCount(n) {
    countBadge.textContent = `${n} ${n === 1 ? 'Enterprise' : 'Enterprises'}`;
}

function showEmptyState(query = '') {
    grid.innerHTML = '';

    const empty = document.createElement('div');
    empty.className = 'empty-state';

    empty.innerHTML = `
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <circle cx="11" cy="11" r="8"/>
            <line x1="21" y1="21" x2="16.65" y2="16.65"/>
        </svg>
        <p>
            ${query
            ? `No enterprises found for "<strong>${escapeHtml(query)}</strong>"`
            : 'No enterprises found.'}
        </p>
    `;

    grid.appendChild(empty);
    updateCount(0);
    updatePagination(1);
}

// ── SEARCH ──
if (searchInput) {
    searchInput.addEventListener('input', function () {
        const query = this.value.trim().toLowerCase();
        filteredEnterprises = allEnterprises.filter(enterprise =>
            (enterprise.StoreName || '').toLowerCase().includes(query) ||
            (enterprise.Username || '').toLowerCase().includes(query)
        );
        currentPage = 1;

        if (filteredEnterprises.length === 0 && query !== '') {
            showEmptyState(query);
        } else {
            renderCards();
        }
    });
}

function updatePagination(totalPages) {
    if (!paginationActions || !prevPageBtn || !nextPageBtn || !pageStatus) return;

    const hasItems = filteredEnterprises && filteredEnterprises.length > 0;
    paginationActions.style.display = hasItems ? 'flex' : 'none';
    pageStatus.textContent = `Page ${currentPage} of ${totalPages}`;
    prevPageBtn.disabled = currentPage <= 1;
    nextPageBtn.disabled = currentPage >= totalPages;
}

if (prevPageBtn) {
    prevPageBtn.addEventListener('click', function () {
        if (currentPage > 1) {
            currentPage--;
            renderCards();
        }
    });
}

if (nextPageBtn) {
    nextPageBtn.addEventListener('click', function () {
        const totalPages = Math.max(1, Math.ceil(filteredEnterprises.length / pageSize));
        if (currentPage < totalPages) {
            currentPage++;
            renderCards();
        }
    });
}

// Helper function to escape HTML
function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

// ── INIT ──
document.addEventListener('DOMContentLoaded', function () {
    loadEnterprises();
});
