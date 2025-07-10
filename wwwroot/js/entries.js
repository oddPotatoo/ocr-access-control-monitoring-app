    // Debounce search input (300ms delay)
    let searchTimer;
    function debounceSearch(value) {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(() => applyFilters(), 300);
    }

    // Updated applyFilters() to include main filter and date range
    function applyFilters() {
        const mainFilter = document.getElementById('mainFilter').value;
    const search = document.getElementById('searchInput').value;
    const status = document.getElementById('statusFilter').value;
    const sort = document.getElementById('sortSelect').value;
    const startDate = document.getElementById('startDate').value;
    const endDate = document.getElementById('endDate').value;

    const params = new URLSearchParams({
        filter: mainFilter,
    search: search,
    status: status,
    sort: sort
        });

    if (mainFilter === 'Range') {
            if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
        }

    // Preserve current page number
    const urlParams = new URLSearchParams(window.location.search);
    const currentPage = urlParams.get('pageNumber');
    if (currentPage) params.append('pageNumber', currentPage);

    window.location.href = `${window.location.pathname}?${params.toString()}`;
    }

    // Remove the duplicate window.location line at the end of the script

    // Update the updateFilterControls function to show/hide relevant controls
    function updateFilterControls() {
        const mainFilter = document.getElementById('mainFilter').value;
    const monthYearGroup = document.getElementById('monthYearGroup');
    const dateRangeGroup = document.getElementById('dateRangeGroup');

    // Hide all groups initially
    monthYearGroup.style.display = 'none';
    dateRangeGroup.style.display = 'none';

    // Show the relevant group based on the selected filter
    if (mainFilter === 'Month' || mainFilter === 'Quarter' || mainFilter === 'Semi-Annual' || mainFilter === 'Year') {
        monthYearGroup.style.display = 'block';
        } else if (mainFilter === 'Range') {
        dateRangeGroup.style.display = 'block';
        }
    }

    // Add to your existing script section
    document.addEventListener('DOMContentLoaded', function() {
        // Initialize date inputs from URL parameters
        const urlParams = new URLSearchParams(window.location.search);
    document.getElementById('startDate').value = '@ViewBag.StartDate';
    document.getElementById('endDate').value = '@ViewBag.EndDate';
    });
