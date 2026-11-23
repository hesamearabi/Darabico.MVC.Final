(() => {
    const menu = document.querySelector('.workspace-menu');
    const navigation = document.getElementById('admin-navigation');
    const backdrop = document.querySelector('.workspace-backdrop');
    const setMenu = open => {
        document.body.classList.toggle('nav-open', open);
        menu?.setAttribute('aria-expanded', String(open));
        if (backdrop) backdrop.hidden = !open;
        if (navigation && window.innerWidth < 1024) navigation.inert = !open;
        if (open) navigation?.querySelector('a')?.focus();
        else menu?.focus();
    };
    menu?.addEventListener('click', () => setMenu(!document.body.classList.contains('nav-open')));
    backdrop?.addEventListener('click', () => setMenu(false));
    if (navigation) {
        const syncNavigation = () => {
            navigation.inert = window.innerWidth < 1024 && !document.body.classList.contains('nav-open');
            if (window.innerWidth >= 1024 && backdrop) backdrop.hidden = true;
        };
        syncNavigation();
        window.addEventListener('resize', syncNavigation);
    }
    document.addEventListener('keydown', event => {
        if (!document.body.classList.contains('nav-open')) return;
        if (event.key === 'Escape') setMenu(false);
        if (event.key === 'Tab' && navigation) {
            const links = [...navigation.querySelectorAll('a,button')];
            const first = links[0], last = links[links.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });
    document.querySelectorAll('.admin-workspace table').forEach(table => {
        if (!table.closest('.table-responsive')) {
            const wrapper = document.createElement('div');
            wrapper.className = 'table-responsive';
            wrapper.tabIndex = 0;
            wrapper.setAttribute('role', 'region');
            wrapper.setAttribute('aria-label', 'جدول داده‌ها؛ برای مشاهده ستون‌ها پیمایش کنید');
            table.before(wrapper);
            wrapper.append(table);
        }
        const body = table.tBodies[0];
        if (body && !body.rows.length) {
            const cell = body.insertRow().insertCell();
            cell.colSpan = table.tHead?.rows[0]?.cells.length || 1;
            cell.className = 'workspace-empty';
            cell.textContent = 'هنوز موردی ثبت نشده است. از فرم یا دکمه ایجاد همین صفحه شروع کنید.';
        }
    });
    document.querySelectorAll('.workspace form label:not([for])').forEach(label => {
        const input = label.parentElement.querySelector('input:not([type=hidden]),select,textarea');
        if (input && !label.contains(input)) {
            if (!input.id) input.id = `workspace-field-${Math.random().toString(36).slice(2)}`;
            label.htmlFor = input.id;
        }
    });
    document.querySelectorAll('form').forEach(form => {
        if (!document.body.classList.contains('workspace') && form.querySelector('.form-input, .form-textarea, .form-select')) form.classList.add('public-form');
        form.addEventListener('submit', event => {
            if (event.defaultPrevented || (window.jQuery && !window.jQuery(form).valid?.() && window.jQuery.validator)) return;
            const button = event.submitter;
            if (!button || button.name || !form.checkValidity()) return;
            window.setTimeout(() => {
                if (event.defaultPrevented) return;
                button.dataset.originalText = button.textContent;
                button.disabled = true;
                button.textContent = 'در حال ثبت…';
                form.setAttribute('aria-busy', 'true');
            }, 0);
        });
    });
    window.addEventListener('pageshow', () => {
        document.querySelectorAll('[data-original-text]').forEach(button => {
            button.disabled = false;
            button.textContent = button.dataset.originalText;
            delete button.dataset.originalText;
            button.form?.removeAttribute('aria-busy');
        });
    });
    document.querySelector('[data-validation-alert]')?.focus();
})();
