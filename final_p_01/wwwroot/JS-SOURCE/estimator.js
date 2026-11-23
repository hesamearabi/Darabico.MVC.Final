(() => {
    const currency = new Intl.NumberFormat('fa-IR', { maximumFractionDigits: 0 });
    document.querySelectorAll('[data-estimator]').forEach(form => {
        const button = form.querySelector('button[type="submit"]');
        const loading = form.querySelector('[data-estimator-loading]');
        const errors = form.querySelector('[data-estimator-errors]');
        const result = form.querySelector('[data-estimator-result]');
        form.addEventListener('input', () => { result.hidden = true; });
        form.addEventListener('submit', async event => {
            event.preventDefault();
            if (button.disabled || !form.reportValidity()) return;
            button.disabled = true;
            loading.hidden = false;
            errors.hidden = true;
            result.hidden = true;
            form.setAttribute('aria-busy', 'true');
            form.querySelectorAll('[data-valmsg-for]').forEach(field => { field.textContent = ''; });
            form.querySelectorAll('[aria-invalid]').forEach(field => field.removeAttribute('aria-invalid'));
            try {
                const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { Accept: 'application/json' } });
                if (!response.headers.get('content-type')?.includes('application/json')) {
                    throw new Error(response.status === 429 ? 'تعداد درخواست‌ها بیش از حد مجاز است. چند دقیقه دیگر دوباره تلاش کنید.' : 'محاسبه انجام نشد. اتصال را بررسی کنید و دوباره تلاش کنید.');
                }
                const data = await response.json();
                if (!response.ok || !data.success) {
                    Object.entries(data.errors || {}).forEach(([name, messages]) => {
                        const field = form.elements.namedItem(name);
                        field?.setAttribute('aria-invalid', 'true');
                        const message = form.querySelector(`[data-valmsg-for="${CSS.escape(name)}"]`);
                        if (message) message.textContent = messages.join(' ');
                    });
                    throw new Error(data.message || 'اطلاعات فرم را بررسی کنید.');
                }
                form.querySelector('[data-estimator-amount]').textContent = `${currency.format(data.result.minCost)} تا ${currency.format(data.result.maxCost)} تومان`;
                form.querySelector('[data-estimator-detail]').textContent = `${currency.format(form.elements.namedItem('CourtCount').value)} زمین · هزینه پایه هر زمین: ${currency.format(data.result.pricePerCourt)} تومان`;
                result.hidden = false;
                result.focus({ preventScroll: true });
                result.scrollIntoView({ block: 'nearest', behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth' });
            } catch (error) {
                errors.textContent = error instanceof TypeError ? 'ارتباط با سرور برقرار نشد. اطلاعات شما حفظ شده؛ دوباره تلاش کنید.' : error.message;
                errors.hidden = false;
                errors.focus();
            } finally {
                button.disabled = false;
                loading.hidden = true;
                form.removeAttribute('aria-busy');
            }
        });
    });
})();
