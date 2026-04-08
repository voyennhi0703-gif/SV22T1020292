// BloomShop — site.js

// Toast helper (also available as window.showToast)
(function () {
    var toastEl = document.getElementById('toast');
    if (toastEl) {
        window.showToast = function (msg, type) {
            var el = document.getElementById('toast');
            if (!el) return;
            el.textContent = msg;
            el.className = 'toast show' + (type ? ' ' + type : '');
            clearTimeout(window._toastTimer);
            window._toastTimer = setTimeout(function () { el.classList.remove('show'); }, 3000);
        };
    }
})();
