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
        let current = 0;
        const show = (index, focus = false) => {
            current = index;
            steps.forEach((step, i) => { step.hidden = i !== index; });
            progress.querySelector('progress').value = index + 1;
            progress.querySelector('[data-step-label]').textContent = index === 0 ? 'مرحله ۱ از ۲' : 'مرحله ۲ از ۲';
            if (focus) steps[index].querySelector('legend').focus();
        };
        const validate = step => {
            const inputs = [...step.querySelectorAll('input, select, textarea')];
            inputs.forEach(input => {
                if (input.hasAttribute('data-digits')) input.value = normalize(input.value).trim();
                if (input.required && input.type !== 'file') input.value = input.value.trim();
                const error = input.closest('.form-field')?.querySelector('.field-validation-error');
                if (!input.validity.valid) {
                    input.setAttribute('aria-invalid', 'true');
                    if (error) { error.classList.remove('field-validation-valid'); error.textContent = input.name === 'Mobile' ? 'شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.' : input.name === 'CourtCount' ? 'تعداد زمین باید بین ۱ تا ۲۰ باشد.' : 'این فیلد را به‌درستی تکمیل کنید.'; }
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
        next.hidden = false;
        back.hidden = false;
        progress.hidden = false;
        steps.forEach(step => step.querySelector('legend').tabIndex = -1);
        show(form.querySelector('[data-valmsg-for="FullName"].field-validation-error:not(:empty), [data-valmsg-for="Mobile"].field-validation-error:not(:empty), [data-valmsg-for="Files"].field-validation-error:not(:empty)') ? 1 : 0);
        next.addEventListener('click', () => { if (validate(steps[0])) show(1, true); });
        back.addEventListener('click', () => show(0, true));
        form.addEventListener('submit', event => {
            for (let i = 0; i < steps.length; i++) {
                show(i);
                if (!validate(steps[i])) { event.preventDefault(); return; }
            }
            show(current);
            submit.disabled = true;
            submit.setAttribute('aria-busy', 'true');
            submit.querySelector('span').textContent = 'در حال ثبت درخواست…';
        });
    });
})();
