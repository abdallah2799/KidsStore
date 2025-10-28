// Products Page JavaScript
document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('searchInput');
    const tableBody = document.querySelector('#productsTable tbody');
    const addBtn = document.getElementById('addProductBtn');
    const addBtnEmpty = document.getElementById('addProductBtnEmpty');
    const saveBtn = document.getElementById('saveProductBtn');
    const modalTitle = document.getElementById('productModalLabel');
    const form = document.getElementById('productForm');
    const productsTable = document.getElementById('productsTable');
    const noProductsPlaceholder = document.getElementById('noProductsPlaceholder');
    const productCodeInput = document.getElementById('productCode');
    const buyingPriceInput = document.getElementById('buyingPrice');
    const sellingPriceInput = document.getElementById('sellingPrice');
    const discountLimitInput = document.getElementById('discountLimit');
    const profitAmountInput = document.getElementById('profitAmount');
    const vendorSelect = document.getElementById('vendorSelect'); // Hidden input for vendor ID
    const vendorSearch = document.getElementById('vendorSearch'); // Search input
    const vendorDropdown = document.getElementById('vendorDropdown'); // Dropdown container
    const vendorClearBtn = document.getElementById('vendorClearBtn');
    const vendorChip = document.getElementById('vendorChip');
    const addInitialStockCheckbox = document.getElementById('addInitialStock');
    const variantSection = document.getElementById('variantSection');
    const variantList = document.getElementById('variantList');
    const btnAddVariantRow = document.getElementById('btnAddVariantRow');

    let variantCounter = 0;
    let allVendors = []; // Store all vendors for autocomplete
    let selectedVendorIndex = -1; // Track keyboard navigation
    let codeAvailable = true; // Track code uniqueness

    // Helper to get season name from enum value
    function getSeasonName(seasonValue) {
        const seasons = {
            1: 'Spring',
            2: 'Summer',
            3: 'Fall',
            4: 'Winter',
            5: 'All Year'
        };
        return seasons[seasonValue] || 'All Year';
    }

    // Debounce helper
    function debounce(fn, delay = 300) {
        let t;
        return (...args) => {
            clearTimeout(t);
            t = setTimeout(() => fn.apply(null, args), delay);
        };
    }

    function setInvalid(input, errorId, message) {
        if (!input) return;
        input.classList.add('is-invalid');
        const el = errorId && document.getElementById(errorId);
        if (el && message) el.textContent = message;
    }

    function clearInvalid(input) {
        if (!input) return;
        input.classList.remove('is-invalid');
    }

    function recomputeProfit() {
        const buy = parseFloat(buyingPriceInput?.value) || 0;
        const sell = parseFloat(sellingPriceInput?.value) || 0;
        const profit = sell - buy;
        if (profitAmountInput) profitAmountInput.value = profit.toFixed(2);
    }

    function validatePrices() {
        const buy = parseFloat(buyingPriceInput?.value);
        const sell = parseFloat(sellingPriceInput?.value);

        let ok = true;
        // Buying must be non-negative number
        if (isNaN(buy) || buy < 0) {
            setInvalid(buyingPriceInput, 'buyingPriceError', 'Buying price must be a valid non-negative number.');
            ok = false;
        } else {
            clearInvalid(buyingPriceInput);
        }
        // Selling must be >= buying
        if (isNaN(sell) || sell < buy) {
            setInvalid(sellingPriceInput, 'sellingPriceError', 'Selling price must be greater than or equal to buying price.');
            ok = false;
        } else {
            clearInvalid(sellingPriceInput);
        }

        recomputeProfit();
        return ok;
    }

    function validateDiscount() {
        const buy = parseFloat(buyingPriceInput?.value) || 0;
        const sell = parseFloat(sellingPriceInput?.value) || 0;
        const disc = parseFloat(discountLimitInput?.value);

        if (isNaN(disc)) {
            clearInvalid(discountLimitInput);
            return true; // treat empty as ok
        }

        // Max discount to keep selling >= buying
        let allowed = 0;
        if (sell > 0) {
            allowed = Math.max(0, ((sell - buy) / sell) * 100);
        }

        if (disc < 0 || disc > 100) {
            setInvalid(discountLimitInput, 'discountError', 'Discount must be between 0 and 100%.');
            return false;
        }

        if (disc - allowed > 0.0001) {
            setInvalid(discountLimitInput, 'discountError', `Discount exceeds maximum allowed (${allowed.toFixed(2)}%).`);
            return false;
        }
        clearInvalid(discountLimitInput);
        return true;
    }

    const checkCodeAvailability = debounce(async () => {
        const code = productCodeInput?.value.trim();
        const id = document.getElementById('productId')?.value || '';
        if (!code) {
            clearInvalid(productCodeInput);
            codeAvailable = false;
            return;
        }
        try {
            const res = await fetch(`/Products/IsCodeAvailable?code=${encodeURIComponent(code)}&excludeId=${encodeURIComponent(id)}`);
            if (!res.ok) throw new Error('Failed code check');
            const data = await res.json();
            codeAvailable = !!data.available;
            if (!codeAvailable) {
                setInvalid(productCodeInput, 'codeError', 'This code is already in use.');
            } else {
                clearInvalid(productCodeInput);
            }
        } catch (e) {
            console.error('Code availability check failed', e);
        }
    }, 400);

    // Helper: Render color dots
    function renderColorDots(colors) {
        if (!Array.isArray(colors) || colors.length === 0) return '-';
        return colors.map(c => `<span class="color-dot" style="background:${c.toLowerCase()}"></span>`).join('');
    }

    // Helper: Render sizes
    function renderSizes(sizes) {
        if (!Array.isArray(sizes) || sizes.length === 0) return '-';
        return sizes.join(', ');
    }

    // Load vendors for autocomplete
    async function loadVendors() {
        try {
            const res = await fetch('/Vendors/GetAll');
            if (res.ok) {
                allVendors = await res.json();
            }
        } catch (err) {
            console.error('Failed to load vendors:', err);
        }
    }
    // Filter and display vendors based on search input
    function filterVendors(searchTerm) {
        if (!searchTerm.trim()) {
            vendorDropdown.classList.remove('show');
            return;
        }

        const term = searchTerm.toLowerCase();
        const filtered = allVendors.filter(v => 
            v.name.toLowerCase().includes(term) || 
            v.id.toString().includes(term)
        );

        renderVendorDropdown(filtered);
    }

    // Render vendor dropdown
    function renderVendorDropdown(vendors) {
        if (vendors.length === 0) {
            vendorDropdown.innerHTML = '<div class="no-results">No vendors found</div>';
            vendorDropdown.classList.add('show');
            return;
        }

        vendorDropdown.innerHTML = vendors.map((v, index) => `
            <div class="vendor-dropdown-item ${index === selectedVendorIndex ? 'active' : ''}" data-vendor-id="${v.id}" data-vendor-name="${v.name}">
                <span class="vendor-name">${v.name}</span>
                <span class="vendor-id">ID: ${v.id}</span>
            </div>
        `).join('');

        vendorDropdown.classList.add('show');

        // Add click handlers to dropdown items
        vendorDropdown.querySelectorAll('.vendor-dropdown-item').forEach(item => {
            item.addEventListener('click', () => {
                selectVendor(item.dataset.vendorId, item.dataset.vendorName);
            });
        });
    }

    // Select a vendor
    function updateVendorChip(vendorName) {
        if (!vendorChip) return;
        if (vendorName && vendorName.trim()) {
            vendorChip.innerHTML = `<i class=\"fas fa-industry\"></i> ${vendorName} <i class=\"fas fa-times remove\" title=\"Clear\"></i>`;
            vendorChip.classList.remove('d-none');
            vendorChip.querySelector('.remove').addEventListener('click', clearVendor);
        } else {
            vendorChip.classList.add('d-none');
            vendorChip.innerHTML = '';
        }
    }

    function selectVendor(vendorId, vendorName) {
        if (vendorSelect) vendorSelect.value = vendorId;
        if (vendorSearch) vendorSearch.value = vendorName;
        if (vendorClearBtn) vendorClearBtn.classList.toggle('d-none', !vendorId);
        updateVendorChip(vendorName);
        vendorDropdown.classList.remove('show');
        selectedVendorIndex = -1;
    }

    function clearVendor() {
        if (vendorSelect) vendorSelect.value = '';
        if (vendorSearch) vendorSearch.value = '';
        updateVendorChip('');
        if (vendorClearBtn) vendorClearBtn.classList.add('d-none');
        vendorDropdown.classList.remove('show');
        vendorSearch && vendorSearch.focus();
    }

    // Vendor search input event
    if (vendorSearch) {
        vendorSearch.addEventListener('input', function() {
            filterVendors(this.value);
            selectedVendorIndex = -1;
        });

        // Show top vendors on focus if empty (better UX)
        vendorSearch.addEventListener('focus', function() {
            const term = this.value.trim();
            if (!term) {
                const top = (allVendors || []).slice().sort((a,b)=> a.name.localeCompare(b.name)).slice(0, 8);
                renderVendorDropdown(top);
            } else {
                filterVendors(term);
            }
        });
        vendorClearBtn && vendorClearBtn.addEventListener('click', clearVendor);
    }

    // Keyboard navigation for vendor dropdown
    vendorSearch && vendorSearch.addEventListener('keydown', function(e) {
        const items = vendorDropdown.querySelectorAll('.vendor-dropdown-item');
        
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            selectedVendorIndex = Math.min(selectedVendorIndex + 1, items.length - 1);
            updateDropdownHighlight();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            selectedVendorIndex = Math.max(selectedVendorIndex - 1, 0);
            updateDropdownHighlight();
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (selectedVendorIndex >= 0 && items[selectedVendorIndex]) {
                const item = items[selectedVendorIndex];
                selectVendor(item.dataset.vendorId, item.dataset.vendorName);
            }
        } else if (e.key === 'Escape') {
            vendorDropdown.classList.remove('show');
            selectedVendorIndex = -1;
        }
    });

    // Update dropdown highlight on keyboard navigation
    function updateDropdownHighlight() {
        const items = vendorDropdown.querySelectorAll('.vendor-dropdown-item');
        items.forEach((item, index) => {
            if (index === selectedVendorIndex) {
                item.classList.add('active');
                item.scrollIntoView({ block: 'nearest' });
            } else {
                item.classList.remove('active');
            }
        });
    }

    // Close dropdown when clicking outside
    document.addEventListener('click', function(e) {
        if (!vendorSearch.contains(e.target) && !vendorDropdown.contains(e.target)) {
            vendorDropdown.classList.remove('show');
            selectedVendorIndex = -1;
        }
    });

    // Keep dropdown open briefly when clicking inside input to allow item clicks
    vendorSearch && vendorSearch.addEventListener('blur', function() {
        setTimeout(() => vendorDropdown.classList.remove('show'), 200);
    });


    // Load products from server
    async function loadProducts() {
        try {
            const res = await fetch('/Products/GetAll');
            if (res.ok) {
                const products = await res.json();
                renderProducts(products);
                updateTabsData(products);
            }
        } catch (err) {
            console.error('Failed to load products:', err);
            ShowToast('Failed to load products', 'error');
        }
    }

    // Render products in main table
    function renderProducts(products) {
        tableBody.innerHTML = '';
        
        if (products.length === 0) {
            noProductsPlaceholder.classList.remove('d-none');
            productsTable.classList.add('d-none');
            return;
        }

        noProductsPlaceholder.classList.add('d-none');
        productsTable.classList.remove('d-none');

    products.forEach(p => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${p.id}</td>
                <td>${p.code}</td>
                <td>${p.description || '-'}</td>
                <td>${p.vendorName || '-'}</td>
                <td><span class="badge bg-info">${getSeasonName(p.season)}</span></td>
                <td>${parseFloat(p.buyingPrice || 0).toFixed(2)}</td>
                <td>${parseFloat(p.sellingPrice || 0).toFixed(2)}</td>
        <td>${(parseFloat(p.sellingPrice || 0) - parseFloat(p.buyingPrice || 0)).toFixed(2)}</td>
                <td>${p.totalStock || 0}</td>
                <td>${renderColorDots(p.colors)}</td>
                <td>${renderSizes(p.sizes)}</td>
                <td>
                    <span class="badge status-badge ${p.isActive ? 'bg-success' : 'bg-secondary'}">${p.isActive ? 'Active' : 'Inactive'}</span>
                </td>
                <td class="text-center">
                    <div class="form-check form-switch d-inline-block align-middle me-2" title="Toggle Active">
                        <input class="form-check-input toggle-active" type="checkbox" data-id="${p.id}" ${p.isActive ? 'checked' : ''}>
                    </div>
                    <button class="btn btn-sm btn-outline-primary me-1 edit-btn" data-bs-toggle="modal" data-bs-target="#productModal">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger delete-btn">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            `;
            
            // Store full data in row
            row.dataset.product = JSON.stringify(p);
            tableBody.appendChild(row);
        });

        bindEditButtons();
        bindDeleteButtons();
        bindToggleActiveAll();
    }

    // Update tabs data based on products
    function updateTabsData(products) {
        // By Season Tab - initially show all
        updateSeasonTable(products, 0);
        
        // Almost Out (stock <= 3 and > 0)
        const almostOutTable = document.querySelector('#almostOutTable tbody');
        if (almostOutTable) {
            almostOutTable.innerHTML = '';
            products.filter(p => (p.totalStock || 0) <= 3 && (p.totalStock || 0) > 0).forEach(p => {
                almostOutTable.innerHTML += `
                    <tr>
                        <td>${p.id}</td>
                        <td>${p.code}</td>
                        <td>${p.description || '-'}</td>
                        <td>${p.vendorName || '-'}</td>
                        <td><span class="badge bg-warning text-dark">${p.totalStock}</span></td>
                        <td><span class="badge bg-warning">Low Stock</span></td>
                    </tr>
                `;
            });
        }

        // Out of Stock (stock == 0)
        const outOfStockTable = document.querySelector('#outOfStockTable tbody');
        if (outOfStockTable) {
            outOfStockTable.innerHTML = '';
            products.filter(p => (p.totalStock || 0) === 0).forEach(p => {
                outOfStockTable.innerHTML += `
                    <tr>
                        <td>${p.id}</td>
                        <td>${p.code}</td>
                        <td>${p.description || '-'}</td>
                        <td>${p.vendorName || '-'}</td>
                        <td><span class="badge bg-danger">${p.totalStock || 0}</span></td>
                    </tr>
                `;
            });
        }

        // Not Moved (30+ days since last sale, or never sold)
        const stagnantTable = document.querySelector('#stagnantTable tbody');
        if (stagnantTable) {
            stagnantTable.innerHTML = '';
            const now = new Date();
            products.filter(p => {
                if (!p.lastSoldAt) return true; // never sold
                const last = new Date(p.lastSoldAt);
                const diffDays = Math.floor((now - last) / (1000 * 60 * 60 * 24));
                return diffDays >= 30;
            }).forEach(p => {
                const lastLabel = p.lastSoldAt ? new Date(p.lastSoldAt).toLocaleDateString() : 'Never';
                stagnantTable.innerHTML += `
                    <tr>
                        <td>${p.id}</td>
                        <td>${p.code}</td>
                        <td>${p.description || '-'}</td>
                        <td>${p.vendorName || '-'}</td>
                        <td>${lastLabel}</td>
                        <td>${p.totalStock || 0}</td>
                    </tr>
                `;
            });
        }

        // Inactive products
        const inactiveTable = document.querySelector('#inactiveTable tbody');
        if (inactiveTable) {
            inactiveTable.innerHTML = '';
            products.filter(p => !p.isActive).forEach(p => {
                inactiveTable.innerHTML += `
                    <tr>
                        <td>${p.id}</td>
                        <td>${p.code}</td>
                        <td>${p.description || '-'}</td>
                        <td>${p.vendorName || '-'}</td>
                        <td>${p.totalStock || 0}</td>
                        <td>
                            <div class="form-check form-switch m-0">
                                <input class="form-check-input toggle-active" type="checkbox" data-id="${p.id}" ${p.isActive ? 'checked' : ''}>
                                <label class="form-check-label small">Activate</label>
                            </div>
                        </td>
                    </tr>
                `;
            });

            // Bind change handler for toggle
            inactiveTable.parentElement.addEventListener('change', async (e) => {
                const t = e.target;
                if (t && t.classList && t.classList.contains('toggle-active')) {
                    const id = t.dataset.id;
                    const isActive = t.checked;
                    try {
                        const res = await fetch(`/Products/ToggleActive/${id}?isActive=${isActive}`, { method: 'PATCH' });
                        if (!res.ok) throw new Error('Toggle failed');
                        // Remove row and refresh lists to reflect new state
                        const row = t.closest('tr');
                        row && row.remove();
                        ShowToast(`Product #${id} ${isActive ? 'activated' : 'deactivated'}`, 'success');
                        // Reload full products to update all tabs
                        await loadProducts();
                    } catch (err) {
                        console.error(err);
                        ShowToast('Could not update product status', 'error');
                        // Revert UI
                        t.checked = !isActive;
                    }
                }
            }, { once: true });
        }
    }

    // 🔹 Filter table rows
    searchInput.addEventListener('keyup', function () {
        document.querySelectorAll('#productsTable tbody tr').forEach(row => {
            row.style.display = row.innerText.toLowerCase().includes(this.value.toLowerCase()) ? '' : 'none';
        });
    });

    // 🔹 Add mode
    [addBtn, addBtnEmpty].forEach(btn => {
        if (btn) {
            btn.addEventListener('click', () => {
                modalTitle.innerHTML = '<i class="fas fa-box-open"></i> Add Product';
                form.reset();
                saveBtn.innerHTML = '<i class="fas fa-save me-1"></i> Save Product';
                document.getElementById('productId').value = '';
                document.getElementById('vendorSearch').value = '';
                document.getElementById('vendorSelect').value = '';
                document.getElementById('isActive').checked = true;
                document.getElementById('seasonSelect').value = '5'; // Default to All Year
                variantSection.classList.add('d-none');
                variantList.innerHTML = '';
                variantCounter = 0;
                clearInvalid(productCodeInput);
                clearInvalid(buyingPriceInput);
                clearInvalid(sellingPriceInput);
                clearInvalid(discountLimitInput);
                codeAvailable = true;
                recomputeProfit();
            });
        }
    });

    // 🔹 Toggle variant section
    addInitialStockCheckbox.addEventListener('change', function () {
        if (this.checked) {
            variantSection.classList.remove('d-none');
        } else {
            variantSection.classList.add('d-none');
        }
    });

    // 🔹 Add variant row
    btnAddVariantRow.addEventListener('click', () => {
        variantCounter++;
        const variantRow = document.createElement('div');
        variantRow.className = 'variant-row d-flex gap-2 align-items-center';
        variantRow.dataset.variantId = variantCounter;
        variantRow.innerHTML = `
            <div style="flex: 0 0 150px;">
                <label class="form-label mb-1 small text-muted">Color</label>
                <div class="position-relative">
                    <input type="text" class="form-control form-control-sm variant-color-name" 
                           id="variant-color-${variantCounter}" placeholder="Select color..." required>
                    <div class="dropdown-menu" id="variant-color-dropdown-${variantCounter}" 
                         style="max-height: 200px; overflow-y: auto;"></div>
                    <input type="hidden" class="variant-color-hex" id="variant-color-hex-${variantCounter}">
                </div>
            </div>
            <div style="flex: 1;">
                <label class="form-label mb-1 small text-muted">Size</label>
                <input type="number" class="form-control form-control-sm variant-size" 
                       placeholder="1-18" min="1" max="18" required>
            </div>
            <div style="flex: 1;">
                <label class="form-label mb-1 small text-muted">Stock</label>
                <input type="number" class="form-control form-control-sm variant-stock" 
                       placeholder="Quantity" min="0" value="0">
            </div>
            <div style="flex: 0 0 40px; padding-top: 24px;">
                <button type="button" class="btn btn-sm btn-outline-danger" 
                        onclick="this.parentElement.parentElement.remove()" 
                        title="Remove variant">
                    <i class="fas fa-trash-alt"></i>
                </button>
            </div>
        `;
        variantList.appendChild(variantRow);
        
        // Setup color autocomplete for this variant
        setupColorAutocomplete(
            `#variant-color-${variantCounter}`, 
            `#variant-color-dropdown-${variantCounter}`, 
            `#variant-color-hex-${variantCounter}`
        );
        
        // Focus on size input for quick entry
        variantRow.querySelector('.variant-size').focus();
    });

    // 🔹 Edit mode
    function bindEditButtons() {
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const row = e.target.closest('tr');
                const product = JSON.parse(row.dataset.product);

                document.getElementById('productId').value = product.id;
                document.getElementById('productCode').value = product.code;
                document.getElementById('productDescription').value = product.description || '';
                document.getElementById('vendorSelect').value = product.vendorId;
                document.getElementById('vendorSearch').value = product.vendorName || '';
                document.getElementById('buyingPrice').value = product.buyingPrice || '';
                document.getElementById('sellingPrice').value = product.sellingPrice || '';
                document.getElementById('discountLimit').value = product.discountLimit || '';
                document.getElementById('isActive').checked = product.isActive;
                document.getElementById('seasonSelect').value = product.season || 5;
                recomputeProfit();
                clearInvalid(productCodeInput);
                clearInvalid(buyingPriceInput);
                clearInvalid(sellingPriceInput);
                clearInvalid(discountLimitInput);
                // Pre-check code availability excluding current ID
                checkCodeAvailability();

                // Load variants if any
                if (product.variants && product.variants.length > 0) {
                    addInitialStockCheckbox.checked = true;
                    variantSection.classList.remove('d-none');
                    variantList.innerHTML = '';
                    product.variants.forEach((v, index) => {
                        variantCounter++;
                        const variantRow = document.createElement('div');
                        variantRow.className = 'variant-row d-flex gap-2 align-items-center';
                        variantRow.dataset.variantId = variantCounter;
                        variantRow.dataset.dbVariantId = v.id || 0; // Store DB ID
                        variantRow.innerHTML = `
                            <div style="flex: 0 0 150px;">
                                <label class="form-label mb-1 small text-muted">Color</label>
                                <div class="position-relative">
                                    <input type="text" class="form-control form-control-sm variant-color-name" 
                                           id="variant-color-${variantCounter}" placeholder="Select color..." 
                                           value="${v.color || ''}" required>
                                    <div class="dropdown-menu" id="variant-color-dropdown-${variantCounter}" 
                                         style="max-height: 200px; overflow-y: auto;"></div>
                                    <input type="hidden" class="variant-color-hex" id="variant-color-hex-${variantCounter}">
                                </div>
                            </div>
                            <div style="flex: 1;">
                                <label class="form-label mb-1 small text-muted">Size</label>
                                <input type="number" class="form-control form-control-sm variant-size" 
                                       placeholder="1-18" value="${v.size || ''}" min="1" max="18" required>
                            </div>
                            <div style="flex: 1;">
                                <label class="form-label mb-1 small text-muted">Stock</label>
                                <input type="number" class="form-control form-control-sm variant-stock" 
                                       placeholder="Quantity" value="${v.stock || 0}" min="0">
                            </div>
                            <div style="flex: 0 0 40px; padding-top: 24px;">
                                <button type="button" class="btn btn-sm btn-outline-danger" 
                                        onclick="this.parentElement.parentElement.remove()" 
                                        title="Remove variant">
                                    <i class="fas fa-trash-alt"></i>
                                </button>
                            </div>
                        `;
                        variantList.appendChild(variantRow);
                        
                        // Setup color autocomplete for this variant
                        setupColorAutocomplete(
                            `#variant-color-${variantCounter}`, 
                            `#variant-color-dropdown-${variantCounter}`, 
                            `#variant-color-hex-${variantCounter}`
                        );
                    });
                }

                modalTitle.innerHTML = '<i class="fas fa-edit"></i> Edit Product';
                saveBtn.innerHTML = '<i class="fas fa-save me-1"></i> Update Product';
            });
        });
    }

    // 🔹 Save product
    saveBtn.addEventListener('click', async () => {
        const id = document.getElementById('productId').value;
        
        const product = {
            Id: id || 0,
            Code: document.getElementById('productCode').value.trim(),
            Description: document.getElementById('productDescription').value.trim(),
            VendorId: parseInt(document.getElementById('vendorSelect').value),
            BuyingPrice: parseFloat(document.getElementById('buyingPrice').value) || 0,
            SellingPrice: parseFloat(document.getElementById('sellingPrice').value) || 0,
            DiscountLimit: parseFloat(document.getElementById('discountLimit').value) || null,
            IsActive: document.getElementById('isActive').checked,
            Season: parseInt(document.getElementById('seasonSelect').value) || 5,
            Variants: []
        };

        if (!product.Code || !product.VendorId) {
            ShowToast('Product code and vendor are required', 'warning');
            return;
        }

        // Client validations
        const priceOk = validatePrices();
        const discountOk = validateDiscount();
        await checkCodeAvailability();
        if (!codeAvailable) {
            ShowToast('Product code must be unique', 'warning');
            return;
        }
        if (!priceOk || !discountOk) {
            ShowToast('Please fix validation errors before saving', 'warning');
            return;
        }

        // Collect variants
        if (addInitialStockCheckbox.checked) {
            const variantRows = variantList.querySelectorAll('.variant-row');
            variantRows.forEach(row => {
                const colorName = row.querySelector('.variant-color-name').value;
                const size = parseInt(row.querySelector('.variant-size').value);
                const stock = parseInt(row.querySelector('.variant-stock').value) || 0;
                const dbVariantId = parseInt(row.dataset.dbVariantId) || 0;
                
                if (colorName && size) {
                    product.Variants.push({ 
                        Id: dbVariantId,
                        Color: colorName, 
                        Size: size, 
                        Stock: stock 
                    });
                }
            });
        }

        const url = id ? '/Products/Update' : '/Products/Add';
        saveBtn.disabled = true;

        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(product)
            });

            if (!res.ok) {
                const errorData = await res.json().catch(() => ({ message: 'Unknown error' }));
                throw new Error(errorData.message || 'Network error');
            }
            
            const result = await res.json();

            if (id) {
                ShowToast(`Product #${id} updated successfully!`, 'success');
            } else {
                ShowToast(`Product #${result.id} added successfully!`, 'success');
            }

            // Reload products
            await loadProducts();

            // ✅ Close modal safely
            const modalEl = document.getElementById('productModal');
            const modalInstance = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            modalInstance.hide();

        } catch (err) {
            console.error('Save error:', err);
            ShowToast(err.message || 'Something went wrong while saving', 'error');
        } finally {
            saveBtn.disabled = false;
        }
    });

    // 🔹 Delete Product
    function bindDeleteButtons() {
        tableBody.addEventListener('click', async (e) => {
            if (e.target.closest('.delete-btn')) {
                const row = e.target.closest('tr');
                const product = JSON.parse(row.dataset.product);
                const id = product.id;

                ShowModalConfirm(
                    `Are you sure you want to delete product <strong>#${id} - ${product.code}</strong>?`,
                    async () => {
                        try {
                            const res = await fetch(`/Products/Delete/${id}`, { method: 'DELETE' });
                            if (res.ok) {
                                row.remove();
                                ShowModalAlert(`Product <strong>#${id}</strong> deleted successfully!`, 'Success', 'success');
                                await loadProducts();
                            } else {
                                ShowModalAlert(`Failed to delete product <strong>#${id}</strong>`, 'Error', 'error');
                            }
                        } catch {
                            ShowModalAlert('Could not connect to server', 'Connection Error', 'error');
                        }
                    },
                    "Delete Product",
                    "Delete",
                    "Cancel"
                );
            }
        });
    }

    // 🔹 Toggle Active in All Products table
    function bindToggleActiveAll() {
        tableBody.addEventListener('change', async (e) => {
            const t = e.target;
            if (t && t.classList && t.classList.contains('toggle-active')) {
                const id = t.dataset.id;
                const isActive = t.checked;
                try {
                    const res = await fetch(`/Products/ToggleActive/${id}?isActive=${isActive}`, { method: 'POST' });
                    if (!res.ok) throw new Error('Toggle failed');

                    const statusBadge = t.closest('tr').querySelector('.status-badge');
                    if (statusBadge) {
                        statusBadge.textContent = isActive ? 'Active' : 'Inactive';
                        statusBadge.classList.toggle('bg-success', isActive);
                        statusBadge.classList.toggle('bg-secondary', !isActive);
                    }

                    ShowToast(`Product #${id} ${isActive ? 'activated' : 'deactivated'}`, 'success');
                    // Refresh other tabs to reflect changes
                    await loadProducts();
                } catch (err) {
                    console.error(err);
                    ShowToast('Could not update product status', 'error');
                    // Revert UI
                    t.checked = !isActive;
                }
            }
        });
    }

    // Focus vendor field and prepopulate suggestions when modal opens
    const productModal = document.getElementById('productModal');
    if (productModal) {
        productModal.addEventListener('shown.bs.modal', () => {
            setTimeout(() => {
                if (vendorSearch) {
                    vendorSearch.focus();
                    const term = vendorSearch.value.trim();
                    if (!term) {
                        const top = (allVendors || []).slice().sort((a,b)=> a.name.localeCompare(b.name)).slice(0, 8);
                        renderVendorDropdown(top);
                    } else {
                        filterVendors(term);
                    }
                }
            }, 100);
        });
    }

    // Initialize
    loadVendors();
    loadProducts();

    // Season filter functionality
    let currentSeasonFilter = 0; // 0 = all seasons
    let allProductsData = []; // Store all products for filtering

    function updateSeasonTable(products, seasonValue) {
        const seasonTable = document.querySelector('#seasonTable tbody');
        const bulkToggleSwitch = document.getElementById('bulkToggleSeasonSwitch');
        const bulkToggleLabel = document.getElementById('bulkToggleLabel');
        
        if (!seasonTable) return;
        
        seasonTable.innerHTML = '';
        
        // Filter products by season
        let filtered = products;
        if (seasonValue > 0) {
            filtered = products.filter(p => p.season === seasonValue);
        }
        
        // Enable/disable bulk action buttons
        const activateBtn = document.getElementById('activateSeasonBtn');
        const deactivateBtn = document.getElementById('deactivateSeasonBtn');
        
        if (activateBtn && deactivateBtn) {
            const shouldDisable = seasonValue === 0 || filtered.length === 0;
            activateBtn.disabled = shouldDisable;
            deactivateBtn.disabled = shouldDisable;
        }
        
        if (filtered.length === 0) {
            seasonTable.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No products found for this season</td></tr>';
            return;
        }
        
        filtered.forEach(p => {
            seasonTable.innerHTML += `
                <tr>
                    <td>${p.id}</td>
                    <td>${p.code}</td>
                    <td>${p.description || '-'}</td>
                    <td>${p.vendorName || '-'}</td>
                    <td><span class="badge bg-info">${getSeasonName(p.season)}</span></td>
                    <td>${p.totalStock || 0}</td>
                    <td><span class="badge ${p.isActive ? 'bg-success' : 'bg-secondary'}">${p.isActive ? 'Active' : 'Inactive'}</span></td>
                </tr>
            `;
        });
    }

    // Season filter buttons
    document.querySelectorAll('.season-filter-btn').forEach(btn => {
        btn.addEventListener('click', async () => {
            // Update active button
            document.querySelectorAll('.season-filter-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            
            const seasonValue = parseInt(btn.dataset.season);
            currentSeasonFilter = seasonValue;
            
            // Reload products to get fresh data
            try {
                const res = await fetch('/Products/GetAll');
                if (res.ok) {
                    allProductsData = await res.json();
                    updateSeasonTable(allProductsData, seasonValue);
                }
            } catch (err) {
                console.error('Failed to load products:', err);
                ShowToast('Failed to load products', 'error');
            }
        });
    });

    // Bulk activate/deactivate buttons
    const activateSeasonBtn = document.getElementById('activateSeasonBtn');
    const deactivateSeasonBtn = document.getElementById('deactivateSeasonBtn');
    
    // Handle activate all
    if (activateSeasonBtn) {
        activateSeasonBtn.addEventListener('click', async () => {
            if (currentSeasonFilter === 0) {
                ShowToast('Please select a specific season first', 'warning');
                return;
            }
            
            const seasonName = getSeasonName(currentSeasonFilter);
            
            ShowModalConfirm(
                `Are you sure you want to <strong>activate</strong> all ${seasonName} products?`,
                async () => {
                    try {
                        const res = await fetch(`/Products/ToggleSeasonActive?season=${currentSeasonFilter}&isActive=true`, { 
                            method: 'POST' 
                        });
                        
                        if (res.ok) {
                            const result = await res.json();
                            ShowModalAlert(
                                `Successfully activated <strong>${result.count}</strong> ${seasonName} products!`,
                                'Success',
                                'success'
                            );
                            
                            // Refresh products
                            await loadProducts();
                            const freshRes = await fetch('/Products/GetAll');
                            if (freshRes.ok) {
                                allProductsData = await freshRes.json();
                                updateSeasonTable(allProductsData, currentSeasonFilter);
                            }
                        } else {
                            const error = await res.text();
                            ShowModalAlert(`Failed to activate products: ${error}`, 'Error', 'error');
                        }
                    } catch (err) {
                        console.error(err);
                        ShowModalAlert('Something went wrong while activating products', 'Error', 'error');
                    }
                },
                'Confirm Activation'
            );
        });
    }
    
    // Handle deactivate all
    if (deactivateSeasonBtn) {
        deactivateSeasonBtn.addEventListener('click', async () => {
            if (currentSeasonFilter === 0) {
                ShowToast('Please select a specific season first', 'warning');
                return;
            }
            
            const seasonName = getSeasonName(currentSeasonFilter);
            
            ShowModalConfirm(
                `Are you sure you want to <strong>deactivate</strong> all ${seasonName} products?`,
                async () => {
                    try {
                        const res = await fetch(`/Products/ToggleSeasonActive?season=${currentSeasonFilter}&isActive=false`, { 
                            method: 'POST' 
                        });
                        
                        if (res.ok) {
                            const result = await res.json();
                            ShowModalAlert(
                                `Successfully deactivated <strong>${result.count}</strong> ${seasonName} products!`,
                                'Success',
                                'success'
                            );
                            
                            // Refresh products
                            await loadProducts();
                            const freshRes = await fetch('/Products/GetAll');
                            if (freshRes.ok) {
                                allProductsData = await freshRes.json();
                                updateSeasonTable(allProductsData, currentSeasonFilter);
                            }
                        } else {
                            const error = await res.text();
                            ShowModalAlert(`Failed to deactivate products: ${error}`, 'Error', 'error');
                        }
                    } catch (err) {
                        console.error(err);
                        ShowModalAlert('Something went wrong while deactivating products', 'Error', 'error');
                    }
                },
                'Confirm Deactivation'
            );
        });
    }

    // Field event bindings for live validation
    if (productCodeInput) {
        productCodeInput.addEventListener('input', checkCodeAvailability);
        productCodeInput.addEventListener('blur', checkCodeAvailability);
    }
    buyingPriceInput && buyingPriceInput.addEventListener('input', () => { validatePrices(); validateDiscount(); });
    sellingPriceInput && sellingPriceInput.addEventListener('input', () => { validatePrices(); validateDiscount(); });
    discountLimitInput && discountLimitInput.addEventListener('input', validateDiscount);
});
