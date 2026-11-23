(() => {
    const button = document.getElementById('loadMoreBtn');
    const grid = document.getElementById('projectsGrid');
    const status = document.getElementById('projectsStatus');
    if (!button || !grid) return;
    button.hidden = false;
    button.addEventListener('click', async () => {
        button.disabled = true;
        grid.setAttribute('aria-busy', 'true');
        status.textContent = 'در حال دریافت پروژه‌ها…';
        const page = Number(button.dataset.page) + 1;
        const query = new URLSearchParams({ page, city: button.dataset.city || '', courtType: button.dataset.courtType || '' });
        try {
            const response = await fetch(`/projects/load-more?${query}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) throw new Error('Projects request failed');
            grid.insertAdjacentHTML('beforeend', await response.text());
            button.dataset.page = String(page);
            button.hidden = response.headers.get('X-Has-More') !== 'true';
            status.textContent = button.hidden ? 'همه پروژه‌ها نمایش داده شد.' : 'پروژه‌های بیشتر اضافه شد.';
        } catch {
            status.textContent = 'دریافت پروژه‌ها انجام نشد. دوباره تلاش کنید.';
        } finally {
            button.disabled = false;
            grid.removeAttribute('aria-busy');
        }
    });
})();
