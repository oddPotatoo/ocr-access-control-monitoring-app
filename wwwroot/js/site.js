// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", function () {
    // Sidebar toggle functionality
    const sidebar = document.querySelector(".sidebar");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const mainContent = document.querySelector(".main-content");

    if (sidebarToggle && sidebar && mainContent) {
        sidebarToggle.addEventListener("click", function () {
            sidebar.classList.toggle("active");
            mainContent.classList.toggle("sidebar-active");
        });
    }

    // LOPEZ GATE toggle functionality
    const lopezGateButton = document.getElementById('lopezGate');
    const additionalNav = document.getElementById('additionalNav');

    if (lopezGateButton && additionalNav) {
        // Retrieve the state from localStorage
        const isNavOpen = localStorage.getItem('isNavOpen') === 'true';

        // Set the initial state based on localStorage
        additionalNav.style.display = isNavOpen ? 'block' : 'none';

        // Add a click event listener to the LOPEZ GATE button
        lopezGateButton.addEventListener('click', function () {
            // Toggle the visibility of the additional navigation
            const isVisible = additionalNav.style.display === 'block';
            additionalNav.style.display = isVisible ? 'none' : 'block';

            // Save the state to localStorage
            localStorage.setItem('isNavOpen', !isVisible);
        });
    }
});