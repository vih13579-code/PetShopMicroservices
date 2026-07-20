(() => {
    const form = document.querySelector('#productForm');
    const container = document.querySelector('#variants');
    const template = document.querySelector('#variantTemplate');
    const error = document.querySelector('#variantError');
    if (!form || !container || !template) return;

    const reindex = () => {
        [...container.querySelectorAll('.variant-row')].forEach((row, index) => {
            row.querySelectorAll('[name]').forEach(input => {
                input.name = input.name.replace(/Variants\[\d+\]/, `Variants[${index}]`);
            });
        });
    };

    document.querySelector('#addVariant').addEventListener('click', () => {
        const index = container.querySelectorAll('.variant-row').length;
        container.insertAdjacentHTML('beforeend', template.innerHTML.replaceAll('__index__', index));
        lucide.createIcons();
    });

    container.addEventListener('click', event => {
        const button = event.target.closest('.remove-variant');
        if (!button) return;
        button.closest('.variant-row').remove();
        reindex();
    });

    form.addEventListener('submit', event => {
        const skus = [...container.querySelectorAll('.sku-input')].map(x => x.value.trim().toLowerCase()).filter(Boolean);
        const duplicate = skus.some((sku, index) => skus.indexOf(sku) !== index);
        error.textContent = duplicate ? 'SKU của các phân loại không được trùng nhau.' : '';
        if (duplicate || !form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
            form.classList.add('was-validated');
        }
    });
})();
