const DashboardDataService = {
    async getStats() {
        try {
            const response = await fetch('/EnterpriseDashboard/GetStats');
            const data = await response.json();

            if (data.success) {
                return {
                    totalSalesToday: data.totalSalesToday,
                    ordersPending: data.ordersPending,
                    deliveriesActive: data.deliveriesActive,
                    newOrdersCount: data.newOrdersCount
                };
            }
            return getDefaultStats();
        } catch (error) {
            console.error('Error fetching stats:', error);
            return getDefaultStats();
        }
    },

    async getChartData(period) {
        try {
            const response = await fetch(`/EnterpriseDashboard/GetChartData?period=${period}`);
            const data = await response.json();

            if (data.success && data.labels && data.labels.length > 0) {
                return {
                    labels: data.labels,
                    values: data.values,
                    total: data.total
                };
            }
            // Pag walang data, magdisplay ng zero
            return getZeroChartData(period);
        } catch (error) {
            console.error('Error fetching chart data:', error);
            return getZeroChartData(period);
        }
    }
};

function getDefaultStats() {
    return {
        totalSalesToday: 0,
        ordersPending: 0,
        deliveriesActive: 0,
        newOrdersCount: 0
    };
}

function getZeroChartData(period) {
    if (period === 'daily' || period === 'weekly') {
        return {
            labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
            values: [0, 0, 0, 0, 0, 0, 0],
            total: period === 'weekly' ? 'Weekly Sales: ₱ 0' : 'This Week: ₱ 0'
        };
    }
    return {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
        values: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
        total: 'This Month: ₱ 0'
    };
}

let currentChart = null;
let currentPeriod = 'daily';

function renderStats(stats) {
    const totalSalesElem = document.querySelector('.stat-total-sales');
    const ordersPendingElem = document.querySelector('.stat-orders-pending');
    const deliveriesActiveElem = document.querySelector('.stat-deliveries-active');
    const newOrdersElem = document.getElementById('newOrdersCount');

    if (totalSalesElem) totalSalesElem.innerText = `₱ ${stats.totalSalesToday.toLocaleString()}`;
    if (ordersPendingElem) ordersPendingElem.innerText = stats.ordersPending;
    if (deliveriesActiveElem) deliveriesActiveElem.innerText = stats.deliveriesActive;
    if (newOrdersElem) newOrdersElem.innerText = stats.newOrdersCount;
}

function renderChart(period, chartData) {
    if (currentChart) currentChart.destroy();

    const canvas = document.getElementById('salesChart');
    if (!canvas) return;

    const isMobile = window.matchMedia('(max-width: 768px)').matches;
    canvas.style.height = isMobile ? '190px' : '250px';

    currentChart = new Chart(canvas, {
        type: 'line',
        data: {
            labels: chartData.labels,
            datasets: [{
                label: 'Sales',
                data: chartData.values,
                borderColor: '#f4c430',
                backgroundColor: 'rgba(244, 196, 48, 0.1)',
                borderWidth: 3,
                tension: 0.4,
                pointRadius: 5,
                pointBackgroundColor: '#0f1a52',
                pointBorderColor: '#f4c430',
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: !isMobile,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return `₱ ${context.raw.toLocaleString()}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: val => `₱${val.toLocaleString()}`
                    }
                }
            }
        }
    });

    const weeklyTotalElem = document.getElementById('weeklyTotal');
    if (weeklyTotalElem) weeklyTotalElem.innerText = chartData.total;
}

// Initialize dashboard
document.addEventListener('DOMContentLoaded', async function () {
    try {
        // Show loading state
        const totalSalesElem = document.querySelector('.stat-total-sales');
        if (totalSalesElem) totalSalesElem.innerText = 'Loading...';

        // Load stats from server
        const stats = await DashboardDataService.getStats();
        renderStats(stats);

        // Load chart data from server
        let chartData = await DashboardDataService.getChartData(currentPeriod);
        renderChart(currentPeriod, chartData);

        // Period selector event
        const periodSelect = document.getElementById('salesPeriodSelect');
        if (periodSelect) {
            periodSelect.addEventListener('change', async (e) => {
                currentPeriod = e.target.value;
                const newData = await DashboardDataService.getChartData(currentPeriod);
                renderChart(currentPeriod, newData);
            });
        }

        // New orders button
        const newOrdersBtn = document.getElementById('newOrdersBtn');
        if (newOrdersBtn) {
            newOrdersBtn.addEventListener('click', () => {
                window.location.href = '/ManageOrders/ManageOrders';
            });
        }

        setInterval(async () => {
            const freshStats = await DashboardDataService.getStats();
            renderStats(freshStats);

            const freshChartData = await DashboardDataService.getChartData(currentPeriod);
            renderChart(currentPeriod, freshChartData);
        }, 10000);

        // Card hover effects
        const cards = document.querySelectorAll('.stats-card');
        cards.forEach(card => {
            card.addEventListener('mouseenter', () => card.style.transform = 'scale(1.02)');
            card.addEventListener('mouseleave', () => card.style.transform = 'scale(1)');
        });
    } catch (error) {
        console.error('Dashboard initialization error:', error);
    }
});
