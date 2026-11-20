// Enzo Book — small progressive enhancements. No frameworks, no inline handlers.
(function () {
    "use strict";

    // Double-submit protection: once a form with data-submitting-text is submitted,
    // disable its submit button so a second click cannot place the order twice.
    document.addEventListener("submit", function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.querySelector("[data-submitting-text]")) {
            return;
        }

        var button = form.querySelector("[data-submitting-text]");
        if (button.dataset.submitted === "true") {
            event.preventDefault();
            return;
        }

        button.dataset.submitted = "true";
        button.disabled = true;
        button.textContent = button.getAttribute("data-submitting-text") || "…";
    });

    // Quantity stepper: the input keeps min/max from the live stock, so the
    // buy button can never ask for more copies than exist.
    var stepper = document.querySelector("[data-stock-watch] ~ .buy-row .qty-stepper");
    var qtyInput = document.getElementById("quantity");

    if (stepper && qtyInput) {
        stepper.addEventListener("click", function (event) {
            var button = event.target.closest("[data-step]");
            if (!button) {
                return;
            }

            var step = parseInt(button.getAttribute("data-step"), 10) || 0;
            var min = parseInt(qtyInput.min, 10) || 1;
            var max = parseInt(qtyInput.max, 10) || 999;
            var next = Math.min(max, Math.max(min, (parseInt(qtyInput.value, 10) || 1) + step));
            qtyInput.value = next;
        });
    }

    // Live stock: refresh so two shoppers never both believe the last copy is theirs.
    // The authoritative guard is the conditional decrement when the order is placed.
    var watch = document.querySelector("[data-stock-watch]");

    if (watch) {
        var url = watch.getAttribute("data-stock-url");
        var badge = document.getElementById("stock-badge");
        var buy = document.getElementById("buy-button");
        var buyLabel = document.getElementById("buy-button-label");
        var baseLabel = buyLabel ? buyLabel.textContent : "";
        var interval = parseInt(watch.getAttribute("data-stock-interval"), 10) || 20000;
        var inFlight = false;

        var render = function (stock) {
            if (badge) {
                badge.textContent = stock <= 0
                    ? "Out of stock"
                    : stock + (stock === 1 ? " copy in stock" : " copies in stock");
                badge.dataset.stock = String(stock);
                badge.classList.toggle("is-in", stock > 10);
                badge.classList.toggle("is-low", stock > 0 && stock <= 10);
                badge.classList.toggle("is-out", stock <= 0);
            }

            if (qtyInput) {
                qtyInput.max = String(Math.max(stock, 1));
                if ((parseInt(qtyInput.value, 10) || 1) > stock) {
                    qtyInput.value = String(Math.max(stock, 1));
                }
            }

            if (buy) {
                buy.disabled = stock <= 0;
            }

            if (buyLabel) {
                buyLabel.textContent = stock <= 0 ? "Out of stock" : baseLabel;
            }
        };

        var refresh = function () {
            if (!url || inFlight || document.hidden) {
                return;
            }

            inFlight = true;

            fetch(url, { headers: { Accept: "application/json" }, cache: "no-store" })
                .then(function (response) {
                    return response.ok ? response.json() : null;
                })
                .then(function (data) {
                    if (data && typeof data.stock === "number") {
                        render(data.stock);
                    }
                })
                .catch(function () {
                    // Keep the last known stock when the network hiccups.
                })
                .finally(function () {
                    inFlight = false;
                });
        };

        window.setInterval(refresh, interval);
        document.addEventListener("visibilitychange", function () {
            if (!document.hidden) {
                refresh();
            }
        });
    }
})();
