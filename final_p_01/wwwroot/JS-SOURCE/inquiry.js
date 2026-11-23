(() => {
    const normalize = value => value.replace(/[۰-۹٠-٩]/g, c => String(c.charCodeAt(0) - (c <= '٩' ? 1632 : 1776)));
    document.querySelectorAll('[data-inquiry-form]').forEach(form => {
        if (form.dataset.initialized) return;
        form.dataset.initialized = 'true';
        const steps = [...form.querySelectorAll('[data-step]')];
        const progress = form.querySelector('[data-progress]');
        const next = form.querySelector('[data-next]');
        const back = form.querySelector('[data-back]');
        const submit = form.querySelector('[data-submit]');
        const files = form.querySelector('[name="Files"]');
        const summary = form.querySelector('[data-summary]');
        if (summary.classList.contains('validation-summary-valid')) {
            summary.replaceChildren();
            summary.classList.remove('validation-summary-valid');
        }
        const dialog = form.parentElement.querySelector('[data-success-dialog]');
        let busy = false;
        const show = (index, focus = false) => {
            steps.forEach((step, i) => { step.hidden = i !== index; });
            progress.querySelector('progress').value = index + 1;
            progress.querySelector('[data-step-label]').textContent = index === 0 ? 'مرحله ۱ از ۲' : 'مرحله ۲ از ۲';
            if (focus) steps[index].querySelector('legend').focus();
        };
        const errorFor = input => input.closest('.dc-field')?.querySelector('[data-valmsg-for]');
        const validate = step => {
            const inputs = [...step.querySelectorAll('input, select, textarea')];
            inputs.forEach(input => {
                if (input.hasAttribute('data-digits')) input.value = normalize(input.value).trim();
                if (input.required && input.type !== 'file') input.value = input.value.trim();
                const error = errorFor(input);
                if (!input.validity.valid) {
                    input.setAttribute('aria-invalid', 'true');
                    if (error) error.textContent = input.name === 'Mobile' ? 'شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.' : input.name === 'CourtCount' ? 'تعداد زمین باید بین ۱ تا ۲۰ باشد.' : input.type === 'file' ? input.validationMessage : 'این فیلد را به‌درستی تکمیل کنید.';
                } else { input.removeAttribute('aria-invalid'); if (error) error.textContent = ''; }
            });
            const invalid = inputs.find(input => !input.validity.valid);
            if (invalid) invalid.focus();
            return !invalid;
        };
        files.addEventListener('change', () => {
            const selected = [...files.files];
            const invalid = selected.length > 5 || selected.some(file => file.size === 0 || file.size > 5 * 1024 * 1024 || !/\.(jpe?g|png|pdf)$/i.test(file.name));
            files.setCustomValidity(invalid ? 'حداکثر ۵ فایل JPG، PNG یا PDF؛ هر فایل حداکثر ۵ مگابایت.' : '');
            form.querySelector('[data-file-feedback]').textContent = invalid ? files.validationMessage : selected.map(file => file.name).join('، ');
        });
        form.querySelectorAll('[data-digits]').forEach(input => input.addEventListener('input', () => { input.value = normalize(input.value); }));
        next.hidden = back.hidden = progress.hidden = false;
        steps.forEach(step => step.querySelector('legend').tabIndex = -1);
        show(steps[1].querySelector('.field-validation-error:not(:empty)') && !steps[0].querySelector('.field-validation-error:not(:empty)') ? 1 : 0);
        const setBusy = value => {
            busy = value;
            submit.disabled = back.disabled = value;
            form.setAttribute('aria-busy', String(value));
            submit.querySelector('span').textContent = value ? 'در حال ثبت درخواست…' : 'ثبت درخواست مشاوره';
        };
        window.addEventListener('pageshow', () => setBusy(false));
        dialog.querySelector('[data-close-dialog]').addEventListener('click', () => dialog.close());
        dialog.addEventListener('close', () => next.focus());
        if (dialog.dataset.submitted === 'true') dialog.showModal();
        next.addEventListener('click', () => { if (validate(steps[0])) show(1, true); });
        back.addEventListener('click', () => show(0, true));
        form.addEventListener('submit', async event => {
            event.preventDefault();
            if (busy) return;
            for (let i = 0; i < steps.length; i++) {
                show(i);
                if (!validate(steps[i])) return;
            }
            summary.textContent = '';
            summary.hidden = true;
            setBusy(true);
            try {
                const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { Accept: 'application/json' } });
                if (response.status === 422) {
                    const data = await response.json();
                    let firstInvalid;
                    const messages = [];
                    Object.entries(data.errors).forEach(([name, errors]) => {
                        messages.push(...errors);
                        const input = form.elements.namedItem(name);
                        if (!input) return;
                        input.setAttribute('aria-invalid', 'true');
                        const error = errorFor(input);
                        if (error) error.textContent = errors.join(' ');
                        firstInvalid ??= input;
                    });
                    summary.textContent = messages.join(' ');
                    summary.hidden = false;
                    if (firstInvalid) {
                        show(steps.findIndex(step => step.contains(firstInvalid)) === 1 ? 1 : 0);
                        firstInvalid.focus();
                    } else summary.focus();
                    return;
                }
                if (!response.ok) throw new Error(response.status === 429 ? 'درخواست‌های زیادی ارسال شده است؛ یک دقیقه دیگر دوباره تلاش کنید.' : 'ثبت درخواست انجام نشد. اطلاعات را بررسی کنید و دوباره تلاش کنید.');
                const data = await response.json();
                if (!data.success) throw new Error('ثبت درخواست تأیید نشد. دوباره تلاش کنید.');
                form.reset();
                files.setCustomValidity('');
                form.querySelectorAll('[data-valmsg-for], [data-file-feedback]').forEach(node => { node.textContent = ''; });
                form.querySelectorAll('[aria-invalid]').forEach(node => node.removeAttribute('aria-invalid'));
                show(0);
                dialog.showModal();
            } catch (error) {
                summary.textContent = error instanceof TypeError ? 'ارتباط با سرور برقرار نشد. اتصال اینترنت را بررسی و دوباره تلاش کنید.' : error.message;
                summary.hidden = false;
                summary.focus();
            } finally { setBusy(false); }
        });
    });
})();
