(() => {
    const inr = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 2 });
    const quotesBySymbol = new Map();
    const priceElements = new Map();

    document.querySelectorAll('[id^="price-"]').forEach((element) => {
        priceElements.set(element.id.substring('price-'.length), element);
    });

    const clock = document.getElementById('local-clock');
    const marketDate = document.getElementById('market-date');
    const updateClock = () => {
        const now = new Date();
        if (clock) {
            clock.textContent = `${new Intl.DateTimeFormat('en-IN', { timeZone: 'Asia/Kolkata', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(now)} IST`;
        }
        if (marketDate) {
            marketDate.innerHTML = `${new Intl.DateTimeFormat('en-IN', { timeZone: 'Asia/Kolkata', weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' }).format(now).toUpperCase()} <span class="live-dot"></span>`;
        }
    };
    updateClock();
    window.setInterval(updateClock, 1000);

    const symbolSelect = document.getElementById('symbol');
    const quantityInput = document.getElementById('quantity');
    const estimatedValue = document.getElementById('estimated-value');
    const submitLabel = document.getElementById('submit-label');
    const updateEstimate = () => {
        if (!symbolSelect || !quantityInput || !estimatedValue) return;
        const quote = quotesBySymbol.get(symbolSelect.value);
        estimatedValue.textContent = quote ? inr.format(quote.price * Number(quantityInput.value || 0)) : '—';
        const selectedSide = document.querySelector('input[name="side"]:checked');
        if (submitLabel && selectedSide) submitLabel.textContent = selectedSide.value;
    };

    symbolSelect?.addEventListener('change', updateEstimate);
    quantityInput?.addEventListener('input', updateEstimate);
    document.querySelectorAll('input[name="side"]').forEach((input) => input.addEventListener('change', updateEstimate));
    document.querySelectorAll('.qty-button').forEach((button) => {
        button.addEventListener('click', () => {
            const step = Number(button.dataset.step);
            quantityInput.value = String(Math.max(1, Number(quantityInput.value || 1) + step));
            updateEstimate();
        });
    });

    const applyQuotes = (quotes) => {
        quotes.forEach((quote) => {
            const symbol = quote.symbol ?? quote.Symbol;
            const price = Number(quote.price ?? quote.Price);
            const change = Number(quote.change ?? quote.Change);
            const percent = Number(quote.changePercent ?? quote.ChangePercent);
            if (!symbol || !Number.isFinite(price)) return;

            quotesBySymbol.set(symbol, { price, change, percent });
            const priceElement = priceElements.get(symbol);
            if (priceElement) priceElement.textContent = inr.format(price);
            const changeElement = document.getElementById(`change-${symbol}`);
            if (changeElement) {
                changeElement.classList.toggle('positive', change >= 0);
                changeElement.classList.toggle('negative', change < 0);
                changeElement.innerHTML = `${change >= 0 ? '▲' : '▼'} ${Math.abs(change).toFixed(2)} <span>(${percent >= 0 ? '+' : ''}${percent.toFixed(2)}%)</span>`;
            }
            if (symbol === 'NIFTY') {
                const chartPrice = document.getElementById('chart-price');
                if (chartPrice) chartPrice.textContent = inr.format(price);
            }
        });
        updateEstimate();
    };

    if (window.signalR) {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/market')
            .withAutomaticReconnect()
            .build();
        connection.on('MarketUpdate', applyQuotes);
        connection.start().catch((error) => console.warn('SignalR market stream unavailable; retrying quotes over HTTP.', error));
    }

    const loadQuoteSnapshot = async () => {
        try {
            const response = await fetch('/api/market/quotes', { headers: { Accept: 'application/json' } });
            if (response.ok) applyQuotes(await response.json());
        } catch (error) {
            console.warn('Unable to refresh simulated market quotes.', error);
        }
    };
    loadQuoteSnapshot();
    window.setInterval(loadQuoteSnapshot, 10000);
})();
