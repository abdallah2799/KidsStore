// ==================== PURCHASES.JS ====================

let vendors = [];
let products = [];
let allInvoices = [];
let allReturns = [];

// ==================== INITIALIZATION ====================
document.addEventListener('DOMContentLoaded', function () {
    initializePage();
    bindEvents();
});

function initializePage() {
    setDefaultDate();
    loadVendors();
    loadProducts();
    loadAllInvoices();
    loadAllReturns();
    loadStatistics();
}

function setDefaultDate() {
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('purchaseDate').value = today;
    document.getElementById('returnDate').value = today;
}

// ==================== VENDOR AUTOCOMPLETE ====================
function loadVendors() {
    fetch('/Vendors/GetAll')
        .then(response => response.json())
        .then(data => {
            vendors = data;
        })
        .catch(() => {
            showError('Failed to load vendors');
        });
}

function setupVendorAutocomplete(inputSelector, dropdownSelector, hiddenIdSelector, onVendorSelected) {
    const input = document.querySelector(inputSelector);
    const dropdown = document.querySelector(dropdownSelector);
    const hiddenId = document.querySelector(hiddenIdSelector);

    input.addEventListener('input', function () {
        const query = this.value.toLowerCase().trim();

        if (query.length < 1) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
            hiddenId.value = '';
            if (onVendorSelected) onVendorSelected(null);
            return;
        }

        const filtered = vendors.filter(v =>
            v.name.toLowerCase().includes(query)
        );

        if (filtered.length === 0) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
            return;
        }

        dropdown.innerHTML = '';
        dropdown.classList.add('show');
        
        filtered.forEach(vendor => {
            const item = document.createElement('a');
            item.className = 'dropdown-item';
            item.textContent = vendor.name;
            item.addEventListener('click', function () {
                input.value = vendor.name;
                hiddenId.value = vendor.id;
                dropdown.classList.remove('show');
                dropdown.innerHTML = '';
                if (onVendorSelected) onVendorSelected(vendor.id);
            });
            dropdown.appendChild(item);
        });
    });

    document.addEventListener('click', function (e) {
        if (!e.target.closest(inputSelector)) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
        }
    });
}

// ==================== PRODUCT AUTOCOMPLETE ====================
function setupProductAutocomplete(inputSelector, dropdownSelector, hiddenIdSelector, onProductSelected) {
    const input = document.querySelector(inputSelector);
    const dropdown = document.querySelector(dropdownSelector);
    const hiddenId = document.querySelector(hiddenIdSelector);

    input.addEventListener('input', function () {
        const query = this.value.toLowerCase().trim();

        if (query.length < 1) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
            hiddenId.value = '';
            if (onProductSelected) onProductSelected(null);
            return;
        }

        const filtered = products.filter(p =>
            p.code.toLowerCase().includes(query) ||
            p.description.toLowerCase().includes(query)
        );

        if (filtered.length === 0) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
            return;
        }

        dropdown.innerHTML = '';
        dropdown.classList.add('show');
        
        filtered.forEach(product => {
            const item = document.createElement('a');
            item.className = 'dropdown-item';
            item.innerHTML = `<strong>${product.code}</strong> - ${product.description}`;
            item.addEventListener('click', function () {
                input.value = `${product.code} - ${product.description}`;
                hiddenId.value = product.id;
                dropdown.classList.remove('show');
                dropdown.innerHTML = '';
                if (onProductSelected) onProductSelected(product);
            });
            dropdown.appendChild(item);
        });
    });

    document.addEventListener('click', function (e) {
        if (!e.target.closest(inputSelector)) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
        }
    });
}

function loadProductsByVendor(vendorId) {
    if (!vendorId) {
        products = [];
        return Promise.resolve();
    }

    return fetch(`/Purchases/GetProductsByVendor?vendorId=${vendorId}`)
        .then(response => response.json())
        .then(data => {
            products = data;
        })
        .catch(() => {
            showError('Failed to load products for this vendor');
            products = [];
        });
}

// ==================== PRODUCTS ====================
function loadProducts() {
    fetch('/Products/GetAll')
        .then(response => response.json())
        .then(data => {
            products = data;
        })
        .catch(() => {
            showError('Failed to load products');
        });
}

// ==================== NEW INVOICE TAB ====================
function bindEvents() {
    // Vendor autocomplete with callback to load products
    setupVendorAutocomplete('#vendorInput', '#vendorDropdown', '#vendorId', function(vendorId) {
        if (vendorId) {
            // Clear existing products when vendor changes
            document.getElementById('productsContainer').innerHTML = '';
            calculateTotal();
            // Load products for this vendor
            loadProductsByVendor(vendorId);
        }
    });
    
    // === NEW RETURN MODAL LOGIC ===
    
    // Load vendors into return vendor select
    loadVendorsForReturn();
    
    // When vendor is selected, load their invoices
    document.getElementById('returnVendorSelect').addEventListener('change', function() {
        const vendorId = this.value;
        const invoiceSelect = document.getElementById('returnInvoiceSelect');
        const saveBtn = document.getElementById('saveReturnBtn');
        
        if (vendorId) {
            loadVendorInvoices(vendorId);
            invoiceSelect.disabled = false;
        } else {
            invoiceSelect.innerHTML = '<option value="">-- First select a vendor --</option>';
            invoiceSelect.disabled = true;
            document.getElementById('returnProductsSection').style.display = 'none';
            saveBtn.disabled = true;
        }
    });
    
    // When invoice is selected, load its products
    document.getElementById('returnInvoiceSelect').addEventListener('change', function() {
        const invoiceId = this.value;
        if (invoiceId) {
            loadInvoiceProducts(invoiceId);
        } else {
            document.getElementById('returnProductsSection').style.display = 'none';
            document.getElementById('saveReturnBtn').disabled = true;
        }
    });
    
    // Select all checkbox
    document.getElementById('selectAllReturnItems').addEventListener('change', function() {
        const checkboxes = document.querySelectorAll('#returnItemsBody input[type="checkbox"]');
        checkboxes.forEach(cb => {
            cb.checked = this.checked;
            const row = cb.closest('tr');
            const qtyInput = row.querySelector('.return-qty-input');
            if (this.checked && qtyInput) {
                const maxQty = parseInt(qtyInput.max);
                qtyInput.value = maxQty; // Set to max purchased quantity
            } else if (qtyInput) {
                qtyInput.value = 0;
            }
        });
        calculateReturnTotal();
    });
    
    // Save return button
    document.getElementById('saveReturnBtn').addEventListener('click', saveReturn);
    
    // Reset modal when closed
    document.getElementById('returnModal').addEventListener('hidden.bs.modal', function() {
        resetReturnModal();
    });
    
    // Initialize return date to today when modal opens
    document.getElementById('returnModal').addEventListener('show.bs.modal', function() {
        document.getElementById('returnDate').value = new Date().toISOString().split('T')[0];
    });

    // Add product block
    document.getElementById('addProductBtn').addEventListener('click', addProductBlock);
    
    // Form submission for invoice
    document.getElementById('invoiceForm').addEventListener('submit', saveInvoice);

    // Clear button
    document.getElementById('clearInvoiceBtn').addEventListener('click', clearInvoiceForm);

    // Tab switches
    const tabButtons = document.querySelectorAll('button[data-bs-toggle="tab"]');
    tabButtons.forEach(button => {
        button.addEventListener('shown.bs.tab', function (e) {
            const target = e.target.getAttribute('data-bs-target');
            if (target === '#allInvoices') {
                loadAllInvoices();
            } else if (target === '#returns') {
                loadAllReturns();
            } else if (target === '#statistics') {
                loadStatistics();
            }
        });
    });
}

// Add a product block with pricing controls and variants table
function addProductBlock() {
    const vendorId = document.getElementById('vendorId').value;
    if (!vendorId) {
        showError('Please select a vendor first');
        return;
    }

    if (products.length === 0) {
        showError('No products available for this vendor');
        return;
    }

    const container = document.getElementById('productsContainer');
    const blockId = `product-block-${Date.now()}`;
    
    const productBlock = document.createElement('div');
    productBlock.className = 'card mb-3 product-block';
    productBlock.id = blockId;
    
    productBlock.innerHTML = `
        <div class="card-header bg-primary bg-gradient text-white">
            <div class="row align-items-center">
                <div class="col-md-4">
                    <label class="form-label text-white mb-0 small">Product *</label>
                    <div class="position-relative">
                        <input type="text" class="form-control form-control-sm product-search" 
                               placeholder="Type product code or name..." autocomplete="off">
                        <input type="hidden" class="product-id">
                        <div class="dropdown-menu product-dropdown" style="width: 100%;"></div>
                    </div>
                </div>
                <div class="col-md-2">
                    <label class="form-label text-white mb-0 small">Buying Price *</label>
                    <input type="number" class="form-control form-control-sm buying-price" 
                           min="0" step="0.01" placeholder="0.00">
                </div>
                <div class="col-md-2">
                    <label class="form-label text-white mb-0 small">Selling Price *</label>
                    <input type="number" class="form-control form-control-sm selling-price" 
                           min="0" step="0.01" placeholder="0.00">
                </div>
                <div class="col-md-2">
                    <label class="form-label text-white mb-0 small">Max Discount</label>
                    <input type="number" class="form-control form-control-sm discount-limit" 
                           min="0" step="0.01" placeholder="0.00">
                </div>
                <div class="col-md-1">
                    <label class="form-label text-white mb-0 small">Profit</label>
                    <div class="profit-display fw-bold" style="font-size: 0.9rem; margin-top: 4px;">0.00</div>
                </div>
                <div class="col-md-1 text-end">
                    <button type="button" class="btn btn-sm btn-danger remove-product-btn" style="margin-top: 20px;">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        </div>
        <div class="card-body">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <h6 class="mb-0">Variants</h6>
                <button type="button" class="btn btn-sm btn-outline-success add-variant-btn">
                    <i class="fas fa-plus"></i> Add Variant
                </button>
            </div>
            <table class="table table-sm table-bordered variants-table">
                <thead class="table-light">
                    <tr>
                        <th style="width: 30%;">Color</th>
                        <th style="width: 20%;">Size</th>
                        <th style="width: 20%;">Quantity</th>
                        <th style="width: 20%;">Subtotal</th>
                        <th style="width: 10%;">Action</th>
                    </tr>
                </thead>
                <tbody>
                    <!-- Variants will be added here -->
                </tbody>
            </table>
            <div class="text-end mt-2">
                <strong>Product Total: <span class="product-total text-success">0.00 EGP</span></strong>
            </div>
        </div>
    `;
    
    container.appendChild(productBlock);
    
    // Setup product autocomplete for this block
    setupProductAutocomplete(productBlock);
    
    // Setup pricing calculations
    setupPricingCalculations(productBlock);
    
    // Setup remove product button
    productBlock.querySelector('.remove-product-btn').addEventListener('click', function() {
        productBlock.remove();
        calculateTotal();
    });
    
    // Setup add variant button
    productBlock.querySelector('.add-variant-btn').addEventListener('click', function() {
        addVariantRow(productBlock);
    });
    
    // Add first variant by default
    addVariantRow(productBlock);
}

// Setup product autocomplete for a product block
function setupProductAutocomplete(productBlock) {
    const searchInput = productBlock.querySelector('.product-search');
    const productIdInput = productBlock.querySelector('.product-id');
    const dropdown = productBlock.querySelector('.product-dropdown');
    const buyingInput = productBlock.querySelector('.buying-price');
    const sellingInput = productBlock.querySelector('.selling-price');
    const discountInput = productBlock.querySelector('.discount-limit');
    
    searchInput.addEventListener('input', function() {
        const query = this.value.toLowerCase().trim();
        
        if (query.length < 1) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
            productIdInput.value = '';
            return;
        }

        const filtered = products.filter(p =>
            p.code.toLowerCase().includes(query) ||
            p.description.toLowerCase().includes(query)
        );

        if (filtered.length === 0) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '';
            return;
        }

        dropdown.innerHTML = '';
        dropdown.classList.add('show');
        
        filtered.forEach(product => {
            const item = document.createElement('a');
            item.className = 'dropdown-item';
            item.innerHTML = `<strong>${product.code}</strong> - ${product.description}`;
            item.addEventListener('click', function() {
                searchInput.value = `${product.code} - ${product.description}`;
                productIdInput.value = product.id;
                dropdown.classList.remove('show');
                dropdown.innerHTML = '';
                
                // Populate pricing from product
                buyingInput.value = product.buyingPrice || 0;
                sellingInput.value = product.sellingPrice || 0;
                discountInput.value = product.discountLimit || 0;
                
                calculateProductProfit(productBlock);
            });
            dropdown.appendChild(item);
        });
    });
}

// Setup pricing calculations for a product block
function setupPricingCalculations(productBlock) {
    const buyingInput = productBlock.querySelector('.buying-price');
    const sellingInput = productBlock.querySelector('.selling-price');
    
    function calculate() {
        calculateProductProfit(productBlock);
        updateProductTotal(productBlock);
    }
    
    buyingInput.addEventListener('input', calculate);
    sellingInput.addEventListener('input', calculate);
}

// Calculate and display profit for a product
function calculateProductProfit(productBlock) {
    const buying = parseFloat(productBlock.querySelector('.buying-price').value) || 0;
    const selling = parseFloat(productBlock.querySelector('.selling-price').value) || 0;
    const profit = selling - buying;
    const profitDisplay = productBlock.querySelector('.profit-display');
    
    profitDisplay.textContent = profit.toFixed(2);
    
    // Color coding
    if (profit < 0) {
        profitDisplay.style.color = '#dc3545'; // Red
    } else if (profit === 0) {
        profitDisplay.style.color = '#ffffff'; // White
    } else {
        profitDisplay.style.color = '#10b981'; // Bright green
    }
}

// Add a variant row to a product block
function addVariantRow(productBlock) {
    const tbody = productBlock.querySelector('.variants-table tbody');
    const row = document.createElement('tr');
    const rowId = `variant-${Date.now()}-${Math.floor(Math.random() * 10000)}`;
    row.id = rowId;
    
    // Color input with palette
    const colorTd = document.createElement('td');
    const colorContainer = document.createElement('div');
    colorContainer.className = 'position-relative';
    
    const colorInputGroup = document.createElement('div');
    colorInputGroup.className = 'input-group input-group-sm';
    
    const colorInput = document.createElement('input');
    colorInput.type = 'text';
    colorInput.className = 'form-control form-control-sm color-input';
    colorInput.placeholder = 'Select or type...';
    colorInput.id = `color-input-${rowId}`;
    
    const swatchAddon = document.createElement('span');
    swatchAddon.className = 'input-group-text p-1';
    swatchAddon.style.width = '38px';
    swatchAddon.style.cursor = 'pointer';
    
    const swatch = document.createElement('span');
    swatch.className = 'color-swatch d-block w-100 h-100';
    swatch.style.borderRadius = '3px';
    swatch.style.backgroundColor = '#fff';
    swatchAddon.appendChild(swatch);
    
    const colorDropdown = document.createElement('div');
    colorDropdown.className = 'dropdown-menu';
    colorDropdown.id = `color-dropdown-${rowId}`;
    
    const colorHexInput = document.createElement('input');
    colorHexInput.type = 'hidden';
    colorHexInput.id = `color-hex-${rowId}`;
    
    colorInputGroup.appendChild(colorInput);
    colorInputGroup.appendChild(swatchAddon);
    colorContainer.appendChild(colorInputGroup);
    colorContainer.appendChild(colorDropdown);
    colorContainer.appendChild(colorHexInput);
    colorTd.appendChild(colorContainer);
    
    // Click swatch to open palette (focus input to trigger palette rendering)
    swatchAddon.addEventListener('click', function(e) {
        e.stopPropagation();
        colorInput.focus();
    });
    
    // Update swatch when color text changes
    colorInput.addEventListener('input', function() {
        const colorName = this.value.trim();
        const hex = (typeof getColorHexFromName === 'function') ? getColorHexFromName(colorName) : '';
        if (hex) {
            swatch.style.backgroundColor = hex;
            colorHexInput.value = hex;
        } else {
            swatch.style.backgroundColor = '#fff';
            colorHexInput.value = '';
        }
    });

    // Also update swatch on change (palette selection dispatches a change event)
    colorInput.addEventListener('change', function() {
        const colorName = this.value.trim();
        const hex = (typeof getColorHexFromName === 'function') ? getColorHexFromName(colorName) : '';
        if (hex) {
            swatch.style.backgroundColor = hex;
            colorHexInput.value = hex;
        } else {
            swatch.style.backgroundColor = '#fff';
            colorHexInput.value = '';
        }
    });
    
    // Size input
    const sizeTd = document.createElement('td');
    const sizeInput = document.createElement('input');
    sizeInput.type = 'text';
    sizeInput.className = 'form-control form-control-sm size-input';
    sizeInput.placeholder = 'Size';
    sizeInput.addEventListener('blur', function() {
        const value = this.value.trim();
        if (value) {
            const numValue = parseInt(value);
            if (!isNaN(numValue) && (numValue < 1 || numValue > 18)) {
                this.style.borderColor = '#dc3545';
                this.title = 'Size should be between 1-18';
            } else {
                this.style.borderColor = '';
                this.title = '';
            }
        }
    });
    sizeTd.appendChild(sizeInput);
    
    // Quantity input
    const qtyTd = document.createElement('td');
    const qtyInput = document.createElement('input');
    qtyInput.type = 'number';
    qtyInput.className = 'form-control form-control-sm';
    qtyInput.min = '1';
    qtyInput.value = '1';
    qtyInput.addEventListener('input', function() {
        updateVariantSubtotal(row, productBlock);
    });
    qtyTd.appendChild(qtyInput);
    
    // Subtotal
    const subtotalTd = document.createElement('td');
    subtotalTd.className = 'text-center';
    const subtotalSpan = document.createElement('span');
    subtotalSpan.className = 'variant-subtotal fw-bold text-success';
    subtotalSpan.textContent = '0.00';
    subtotalTd.appendChild(subtotalSpan);
    
    // Delete button
    const actionTd = document.createElement('td');
    actionTd.className = 'text-center';
    const deleteBtn = document.createElement('button');
    deleteBtn.type = 'button';
    deleteBtn.className = 'btn btn-sm btn-danger';
    deleteBtn.innerHTML = '<i class="fas fa-trash"></i>';
    deleteBtn.addEventListener('click', function() {
        row.remove();
        updateProductTotal(productBlock);
    });
    actionTd.appendChild(deleteBtn);
    
    row.appendChild(colorTd);
    row.appendChild(sizeTd);
    row.appendChild(qtyTd);
    row.appendChild(subtotalTd);
    row.appendChild(actionTd);
    
    tbody.appendChild(row);
    
    // Setup color picker for this row
    setupColorAutocomplete(`#color-input-${rowId}`, `#color-dropdown-${rowId}`, `#color-hex-${rowId}`);
    
    // Initial subtotal calculation
    updateVariantSubtotal(row, productBlock);
}

// Update variant subtotal
function updateVariantSubtotal(variantRow, productBlock) {
    const qty = parseInt(variantRow.querySelector('input[type="number"]').value) || 0;
    const buyingPrice = parseFloat(productBlock.querySelector('.buying-price').value) || 0;
    const subtotal = qty * buyingPrice;
    
    variantRow.querySelector('.variant-subtotal').textContent = subtotal.toFixed(2);
    
    updateProductTotal(productBlock);
}

// Update product total (sum of all variants)
function updateProductTotal(productBlock) {
    let total = 0;
    const subtotals = productBlock.querySelectorAll('.variant-subtotal');
    subtotals.forEach(span => {
        total += parseFloat(span.textContent) || 0;
    });
    
    productBlock.querySelector('.product-total').textContent = total.toFixed(2) + ' EGP';
    
    calculateTotal();
}

function addInvoiceItemRow() {
    const vendorId = document.getElementById('vendorId').value;
    if (!vendorId) {
        showError('Please select a vendor first');
        return;
    }

    if (products.length === 0) {
        showError('No products available for this vendor');
        return;
    }

    const tbody = document.querySelector('#invoiceItemsTable tbody');
    const row = document.createElement('tr');
    const rowId = `row-${Date.now()}`;
    row.id = rowId;

    // Product autocomplete container
    const productContainer = document.createElement('div');
    productContainer.className = 'position-relative';
    
    const productInput = document.createElement('input');
    productInput.type = 'text';
    productInput.className = 'form-control product-input';
    productInput.placeholder = 'Start typing...';
    
    const productDropdown = document.createElement('div');
    productDropdown.className = 'dropdown-menu';
    productDropdown.style.maxHeight = '200px';
    productDropdown.style.overflowY = 'auto';
    
    const productHiddenId = document.createElement('input');
    productHiddenId.type = 'hidden';
    productHiddenId.className = 'product-id';
    
    productContainer.appendChild(productInput);
    productContainer.appendChild(productDropdown);
    productContainer.appendChild(productHiddenId);

    let selectedProduct = null;

    // Setup product autocomplete for this row
    productInput.addEventListener('input', function() {
        const query = this.value.toLowerCase().trim();
        
        if (query.length < 1) {
            productDropdown.classList.remove('show');
            productDropdown.innerHTML = '';
            productHiddenId.value = '';
            selectedProduct = null;
            return;
        }

        const filtered = products.filter(p =>
            p.code.toLowerCase().includes(query) ||
            p.description.toLowerCase().includes(query)
        );

        if (filtered.length === 0) {
            productDropdown.classList.remove('show');
            productDropdown.innerHTML = '';
            return;
        }

        productDropdown.innerHTML = '';
        productDropdown.classList.add('show');
        
        filtered.forEach(product => {
            const item = document.createElement('a');
            item.className = 'dropdown-item';
            item.innerHTML = `<strong>${product.code}</strong> - ${product.description}`;
            item.addEventListener('click', function() {
                productInput.value = `${product.code} - ${product.description}`;
                productHiddenId.value = product.id;
                selectedProduct = product;
                productDropdown.classList.remove('show');
                productDropdown.innerHTML = '';
                
                // Populate pricing fields from product
                buyingPriceInput.value = product.buyingPrice || 0;
                sellingPriceInput.value = product.sellingPrice || 0;
                discountInput.value = product.discountLimit || 0;
                
                calculateProfit();
                updateSubtotal();
            });
            productDropdown.appendChild(item);
        });
    });

    // Color input with palette picker
    const colorContainer = document.createElement('div');
    colorContainer.className = 'position-relative';
    
    const colorInputGroup = document.createElement('div');
    colorInputGroup.className = 'input-group input-group-sm';
    
    const colorInput = document.createElement('input');
    colorInput.type = 'text';
    colorInput.className = 'form-control color-input';
    colorInput.placeholder = 'Select or type color...';
    colorInput.id = `color-input-${rowId}`;
    colorInput.style.cursor = 'text';
    
    // Color preview swatch
    const swatchAddon = document.createElement('span');
    swatchAddon.className = 'input-group-text p-1';
    swatchAddon.style.width = '38px';
    swatchAddon.style.cursor = 'pointer';
    swatchAddon.title = 'Click to open color palette';
    
    const swatch = document.createElement('span');
    swatch.className = 'color-swatch d-block w-100 h-100';
    swatch.style.borderRadius = '3px';
    swatch.style.backgroundColor = '#fff';
    
    swatchAddon.appendChild(swatch);
    
    const colorDropdown = document.createElement('div');
    colorDropdown.className = 'dropdown-menu';
    colorDropdown.id = `color-dropdown-${rowId}`;
    
    const colorHexInput = document.createElement('input');
    colorHexInput.type = 'hidden';
    colorHexInput.className = 'color-hex';
    colorHexInput.id = `color-hex-${rowId}`;
    
    colorInputGroup.appendChild(colorInput);
    colorInputGroup.appendChild(swatchAddon);
    colorContainer.appendChild(colorInputGroup);
    colorContainer.appendChild(colorDropdown);
    colorContainer.appendChild(colorHexInput);
    
    // Click swatch to open palette
    swatchAddon.addEventListener('click', function(e) {
        e.stopPropagation();
        colorInput.focus();
    });
    
    // Update swatch when color changes
    colorInput.addEventListener('change', function() {
        const hex = getColorHexFromName(this.value);
        if (hex) {
            swatch.style.backgroundColor = hex;
            colorHexInput.value = hex;
        }
    });

    // Size input (allows 1-18 or custom text)
    const sizeInput = document.createElement('input');
    sizeInput.type = 'text';
    sizeInput.className = 'form-control size-input';
    sizeInput.placeholder = 'Size';
    
    // Validation: warn if not 1-18
    sizeInput.addEventListener('blur', function() {
        const value = this.value.trim();
        if (value) {
            const numValue = parseInt(value);
            if (!isNaN(numValue) && (numValue < 1 || numValue > 18)) {
                this.style.borderColor = '#dc3545';
                this.title = 'Size should be between 1-18';
            } else {
                this.style.borderColor = '';
                this.title = '';
            }
        }
    });

    // Quantity input
    const qtyInput = document.createElement('input');
    qtyInput.type = 'number';
    qtyInput.className = 'form-control form-control-sm';
    qtyInput.min = '1';
    qtyInput.value = '1';
    qtyInput.addEventListener('input', updateSubtotal);

    // Buying Price input
    const buyingPriceInput = document.createElement('input');
    buyingPriceInput.type = 'number';
    buyingPriceInput.className = 'form-control form-control-sm buying-price-input';
    buyingPriceInput.min = '0';
    buyingPriceInput.step = '0.01';
    buyingPriceInput.placeholder = '0.00';
    buyingPriceInput.addEventListener('input', function() {
        calculateProfit();
        updateSubtotal();
    });

    // Selling Price input
    const sellingPriceInput = document.createElement('input');
    sellingPriceInput.type = 'number';
    sellingPriceInput.className = 'form-control form-control-sm selling-price-input';
    sellingPriceInput.min = '0';
    sellingPriceInput.step = '0.01';
    sellingPriceInput.placeholder = '0.00';
    sellingPriceInput.addEventListener('input', calculateProfit);

    // Discount Limit input
    const discountInput = document.createElement('input');
    discountInput.type = 'number';
    discountInput.className = 'form-control form-control-sm discount-input';
    discountInput.min = '0';
    discountInput.step = '0.01';
    discountInput.placeholder = '0.00';

    // Profit display (readonly)
    const profitSpan = document.createElement('span');
    profitSpan.className = 'profit-cell fw-bold';
    profitSpan.style.fontSize = '0.85rem';
    profitSpan.textContent = '0.00';

    // Subtotal
    const subtotalSpan = document.createElement('span');
    subtotalSpan.className = 'subtotal-cell';
    subtotalSpan.textContent = '0.00';

    // Delete button
    const deleteBtn = document.createElement('button');
    deleteBtn.type = 'button';
    deleteBtn.className = 'btn btn-sm btn-danger';
    deleteBtn.innerHTML = '<i class="fas fa-trash"></i>';
    deleteBtn.addEventListener('click', function() {
        row.remove();
        calculateTotal();
    });

    // Calculate profit function
    function calculateProfit() {
        const buying = parseFloat(buyingPriceInput.value) || 0;
        const selling = parseFloat(sellingPriceInput.value) || 0;
        const profit = selling - buying;
        
        profitSpan.textContent = profit.toFixed(2);
        
        // Color coding
        if (profit < 0) {
            profitSpan.style.color = '#dc3545'; // Red
        } else if (profit === 0) {
            profitSpan.style.color = '#6c757d'; // Gray
        } else {
            profitSpan.style.color = '#059669'; // Green
        }
    }

    // Update subtotal function
    function updateSubtotal() {
        const qty = parseInt(qtyInput.value) || 0;
        const buyingPrice = parseFloat(buyingPriceInput.value) || 0;
        const sub = qty * buyingPrice;
        subtotalSpan.textContent = sub.toFixed(2);
        calculateTotal();
    }

    // Store row data for later retrieval
    row.dataset.getItemData = function() {
        return {
            productId: parseInt(productHiddenId.value),
            product: selectedProduct,
            color: colorInput.value.trim(),
            size: sizeInput.value.trim(),
            quantity: parseInt(qtyInput.value),
            buyingPrice: parseFloat(buyingPriceInput.value) || 0,
            sellingPrice: parseFloat(sellingPriceInput.value) || 0,
            discountLimit: parseFloat(discountInput.value) || 0
        };
    };

    // Append cells
    row.appendChild(createCell(productContainer));
    row.appendChild(createCell(colorContainer));
    row.appendChild(createCell(sizeInput));
    row.appendChild(createCell(qtyInput));
    row.appendChild(createCell(buyingPriceInput));
    row.appendChild(createCell(sellingPriceInput));
    row.appendChild(createCell(discountInput));
    row.appendChild(createCell(profitSpan));
    row.appendChild(createCell(subtotalSpan));
    row.appendChild(createCell(deleteBtn));

    tbody.appendChild(row);
    
    // Setup color autocomplete AFTER the row is added to DOM
    setupColorAutocomplete(`#color-input-${rowId}`, `#color-dropdown-${rowId}`, `#color-hex-${rowId}`);
}

function createCell(content) {
    const td = document.createElement('td');
    td.appendChild(content);
    return td;
}

function calculateTotal() {
    let total = 0;
    const productTotals = document.querySelectorAll('.product-total');
    productTotals.forEach(span => {
        const text = span.textContent.replace(' EGP', '');
        total += parseFloat(text) || 0;
    });
    document.getElementById('totalAmount').textContent = total.toFixed(2) + ' EGP';
}

function saveInvoice(e) {
    e.preventDefault();

    const vendorId = parseInt(document.getElementById('vendorId').value);
    const purchaseDate = document.getElementById('purchaseDate').value;

    if (!vendorId) {
        showError('Please select a vendor');
        return;
    }

    const productBlocks = document.querySelectorAll('.product-block');
    if (productBlocks.length === 0) {
        showError('Please add at least one product');
        return;
    }

    // Collect and validate all products and variants
    const itemsData = [];
    const productPrices = {};

    for (const block of productBlocks) {
        const productId = parseInt(block.querySelector('.product-id').value);
        const buyingPrice = parseFloat(block.querySelector('.buying-price').value);
        const sellingPrice = parseFloat(block.querySelector('.selling-price').value);
        const discountLimit = parseFloat(block.querySelector('.discount-limit').value) || 0;

        if (!productId) {
            showError('Please select a product for all product blocks');
            return;
        }

        if (!buyingPrice || buyingPrice <= 0) {
            showError('Please enter a valid buying price for all products');
            return;
        }

        if (!sellingPrice || sellingPrice <= 0) {
            showError('Please enter a valid selling price for all products');
            return;
        }

        // Validate profit is not negative
        const profit = sellingPrice - buyingPrice;
        if (profit < 0) {
            showError('Profit cannot be negative! Selling price must be greater than or equal to buying price.');
            return;
        }

        // Store product pricing
        productPrices[productId] = { buyingPrice, sellingPrice, discountLimit };

        // Get all variants for this product
        const variantRows = block.querySelectorAll('.variants-table tbody tr');
        
        if (variantRows.length === 0) {
            showError('Please add at least one variant for each product');
            return;
        }

        for (const row of variantRows) {
            const colorName = row.querySelector('.color-input').value.trim();
            const colorHex = row.querySelector('input[type="hidden"]').value.trim();
            const size = row.querySelector('.size-input').value.trim();
            const quantity = parseInt(row.querySelector('input[type="number"]').value);

            if (!colorHex || !size) {
                showError('Please fill all variant fields correctly');
                return;
            }

            if (quantity <= 0 || isNaN(quantity)) {
                showError('Variant quantity must be greater than 0');
                return;
            }

            itemsData.push({
                productId,
                color: colorHex, // Use hex value instead of name
                size,
                quantity,
                buyingPrice,
                sellingPrice,
                discountLimit
            });
        }
    }

    // Process items
    processInvoiceItems(itemsData, productPrices, vendorId, purchaseDate);
}

async function processInvoiceItems(itemsData, productPrices, vendorId, purchaseDate) {
    try {
        const items = [];

        // Update product prices for each unique product
        for (const [productId, prices] of Object.entries(productPrices)) {
            await updateProductPrices(parseInt(productId), prices.buyingPrice, prices.sellingPrice, prices.discountLimit);
        }

        // For each item, ensure variant exists
        for (const item of itemsData) {
            const variant = await getOrCreateVariant(item.productId, item.color, item.size);
            
            if (!variant || !variant.id) {
                throw new Error(`Failed to create/get variant for product ${item.productId}`);
            }

            items.push({
                productVariantId: variant.id,
                quantity: item.quantity,
                buyingPrice: item.buyingPrice
            });
        }

        // Now save the invoice
        const total = items.reduce((sum, item) => sum + (item.quantity * item.buyingPrice), 0);

        const invoice = {
            vendorId: vendorId,
            purchaseDate: purchaseDate,
            totalAmount: total,
            items: items
        };

        const response = await fetch('/Purchases/AddInvoice', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(invoice)
        });

        if (!response.ok) throw new Error('Failed to save invoice');

        const data = await response.json();
        showSuccess('Invoice saved successfully!');
        clearInvoiceForm();
        loadAllInvoices();

    } catch (error) {
        showError(error.message || 'Failed to save invoice');
    }
}

// Update product prices
async function updateProductPrices(productId, buyingPrice, sellingPrice, discountLimit) {
    try {
        const response = await fetch(`/Products/UpdatePrices/${productId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                buyingPrice: buyingPrice,
                sellingPrice: sellingPrice,
                discountLimit: discountLimit
            })
        });

        if (!response.ok) throw new Error('Failed to update product prices');
        
        // Update local product data
        const product = products.find(p => p.id === productId);
        if (product) {
            product.buyingPrice = buyingPrice;
            product.sellingPrice = sellingPrice;
            product.discountLimit = discountLimit;
        }
    } catch (error) {
        console.error('Error updating product prices:', error);
        throw error;
    }
}

async function getOrCreateVariant(productId, color, size) {
    // First check if variant exists in the loaded product data
    const product = products.find(p => p.id === productId);
    if (product && product.variants) {
        const existing = product.variants.find(v => 
            v.color.toLowerCase() === color.toLowerCase() && v.size.toString() === size.toString()
        );
        if (existing) {
            return existing;
        }
    }

    // Variant doesn't exist, create it
    try {
        const response = await fetch('/Purchases/CreateVariant', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                productId: productId,
                color: color,
                size: size
            })
        });

        if (!response.ok) throw new Error('Failed to create variant');

        const variant = await response.json();
        
        // Add to product variants cache
        if (product) {
            if (!product.variants) product.variants = [];
            product.variants.push(variant);
        }

        return variant;

    } catch (error) {
        throw new Error(`Failed to create variant: ${error.message}`);
    }
}

function clearInvoiceForm() {
    document.getElementById('invoiceForm').reset();
    document.getElementById('vendorId').value = '';
    document.getElementById('productsContainer').innerHTML = '';
    document.getElementById('totalAmount').textContent = '0.00 EGP';
    setDefaultDate();
}

// ==================== ALL INVOICES TAB ====================
function loadAllInvoices() {
    fetch('/Purchases/GetAllInvoices')
        .then(response => response.json())
        .then(data => {
            allInvoices = data;
            renderInvoicesTable();
        })
        .catch(() => {
            showError('Failed to load invoices');
        });
}

function renderInvoicesTable() {
    const tbody = document.querySelector('#allInvoicesTable tbody');
    tbody.innerHTML = '';

    if (allInvoices.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="text-center empty-state"><i class="fas fa-inbox"></i><h5>No invoices yet</h5></td></tr>';
        return;
    }

    allInvoices.forEach((inv, index) => {
        const row = document.createElement('tr');
        row.className = 'fade-in';
        row.innerHTML = `
            <td>${index + 1}</td>
            <td>#${inv.id}</td>
            <td>${inv.vendorName || 'N/A'}</td>
            <td>${formatDate(inv.purchaseDate)}</td>
            <td class="fw-bold text-success">${inv.totalAmount.toFixed(2)} EGP</td>
            <td>${inv.items.length}</td>
            <td class="text-center">
                <button class="btn btn-sm btn-info btn-action" onclick="viewInvoice(${inv.id})">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn btn-sm btn-danger btn-action" onclick="deleteInvoice(${inv.id})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function viewInvoice(id) {
    const invoice = allInvoices.find(inv => inv.id === id);
    if (!invoice) return;

    let itemsHtml = '<table class="table table-sm mt-3"><thead><tr><th>Product</th><th>Color</th><th>Size</th><th>Qty</th><th>Price</th><th>Subtotal</th></tr></thead><tbody>';

    invoice.items.forEach(item => {
        itemsHtml += `
            <tr>
                <td>${item.productCode} - ${item.productDescription}</td>
                <td>${item.color}</td>
                <td>${item.size}</td>
                <td>${item.quantity}</td>
                <td>${item.buyingPrice.toFixed(2)}</td>
                <td>${item.subtotal.toFixed(2)}</td>
            </tr>
        `;
    });

    itemsHtml += '</tbody></table>';

    const content = `
        <div class="detail-row">
            <span class="detail-label">Invoice No:</span>
            <span class="detail-value">#${invoice.id}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Vendor:</span>
            <span class="detail-value">${invoice.vendorName}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Purchase Date:</span>
            <span class="detail-value">${formatDate(invoice.purchaseDate)}</span>
        </div>
        <div class="detail-row">
            <span class="detail-label">Total Amount:</span>
            <span class="detail-value fw-bold text-success">${invoice.totalAmount.toFixed(2)} EGP</span>
        </div>
        <h6 class="mt-3">Items:</h6>
        ${itemsHtml}
    `;

    document.getElementById('invoiceDetailsContent').innerHTML = content;
    const modal = new bootstrap.Modal(document.getElementById('invoiceDetailsModal'));
    modal.show();
}

function deleteInvoice(id) {
    ShowModalConfirm('Are you sure you want to delete this invoice? Stock will be reverted.', async () => {
        try {
            const response = await fetch(`/Purchases/DeleteInvoice/${id}`, { method: 'DELETE' });
            if (!response.ok) throw new Error('Failed to delete invoice');
            showSuccess('Invoice deleted successfully');
            loadAllInvoices();
        } catch (err) {
            showError(err.message || 'Failed to delete invoice');
        }
    }, 'Delete Invoice', 'Delete', 'Cancel');
}

// ==================== RETURNS TAB ====================
function addReturnItemRow() {
    const tbody = document.querySelector('#returnItemsTable tbody');
    const row = document.createElement('tr');

    // Create similar structure to invoice items
    const productSelect = document.createElement('select');
    productSelect.className = 'form-select product-select';
    productSelect.innerHTML = '<option value="">Select Product</option>';
    products.forEach(p => {
        const option = document.createElement('option');
        option.value = p.id;
        option.textContent = `${p.code} - ${p.description}`;
        option.dataset.variants = JSON.stringify(p.variants || []);
        productSelect.appendChild(option);
    });

    const colorSelect = document.createElement('select');
    colorSelect.className = 'form-select variant-select';
    colorSelect.innerHTML = '<option value="">Select Color</option>';

    const sizeSelect = document.createElement('select');
    sizeSelect.className = 'form-select variant-select';
    sizeSelect.innerHTML = '<option value="">Select Size</option>';

    const qtyInput = document.createElement('input');
    qtyInput.type = 'number';
    qtyInput.className = 'form-control';
    qtyInput.min = '1';
    qtyInput.value = '1';

    const priceInput = document.createElement('input');
    priceInput.type = 'number';
    priceInput.className = 'form-control';
    priceInput.min = '0';
    priceInput.step = '0.01';
    priceInput.value = '0';

    const subtotalSpan = document.createElement('span');
    subtotalSpan.className = 'subtotal-cell';
    subtotalSpan.textContent = '0.00';

    const deleteBtn = document.createElement('button');
    deleteBtn.type = 'button';
    deleteBtn.className = 'btn btn-sm btn-danger';
    deleteBtn.innerHTML = '<i class="fas fa-trash"></i>';
    deleteBtn.addEventListener('click', function () {
        row.remove();
        calculateReturnTotal();
    });

    productSelect.addEventListener('change', function () {
        const selectedOption = this.options[this.selectedIndex];
        const variants = JSON.parse(selectedOption.dataset.variants || '[]');

        colorSelect.innerHTML = '<option value="">Select Color</option>';
        sizeSelect.innerHTML = '<option value="">Select Size</option>';

        if (variants.length > 0) {
            const colors = [...new Set(variants.map(v => v.color))];
            colors.forEach(color => {
                const option = document.createElement('option');
                option.value = color;
                option.textContent = color;
                colorSelect.appendChild(option);
            });
        }
    });

    colorSelect.addEventListener('change', function () {
        const selectedOption = productSelect.options[productSelect.selectedIndex];
        const variants = JSON.parse(selectedOption.dataset.variants || '[]');
        const selectedColor = this.value;

        sizeSelect.innerHTML = '<option value="">Select Size</option>';

        if (selectedColor) {
            const sizes = variants.filter(v => v.color === selectedColor);
            sizes.forEach(s => {
                const option = document.createElement('option');
                option.value = s.id;
                option.textContent = s.size;
                sizeSelect.appendChild(option);
            });
        }
    });

    function updateSubtotal() {
        const qty = parseInt(qtyInput.value) || 0;
        const price = parseFloat(priceInput.value) || 0;
        const sub = qty * price;
        subtotalSpan.textContent = sub.toFixed(2);
        calculateReturnTotal();
    }

    qtyInput.addEventListener('input', updateSubtotal);
    priceInput.addEventListener('input', updateSubtotal);

    row.appendChild(createCell(productSelect));
    row.appendChild(createCell(colorSelect));
    row.appendChild(createCell(sizeSelect));
    row.appendChild(createCell(qtyInput));
    row.appendChild(createCell(priceInput));
    row.appendChild(createCell(subtotalSpan));
    row.appendChild(createCell(deleteBtn));

    tbody.appendChild(row);
}

function calculateReturnTotal() {
    let total = 0;
    const subtotals = document.querySelectorAll('#returnItemsTable .subtotal-cell');
    subtotals.forEach(span => {
        total += parseFloat(span.textContent) || 0;
    });
    document.getElementById('totalRefund').textContent = total.toFixed(2) + ' EGP';
}

// === NEW RETURN FUNCTIONS ===

async function loadVendorsForReturn() {
    try {
        const response = await fetch('/Vendors/GetAll');
        const vendors = await response.json();
        
        const select = document.getElementById('returnVendorSelect');
        select.innerHTML = '<option value="">-- Choose Vendor --</option>';
        
        vendors.forEach(vendor => {
            const option = document.createElement('option');
            option.value = vendor.id;
            option.textContent = vendor.name;
            select.appendChild(option);
        });
    } catch (error) {
        console.error('Failed to load vendors:', error);
        showError('Failed to load vendors');
    }
}

async function loadVendorInvoices(vendorId) {
    try {
        const response = await fetch(`/Purchases/GetInvoicesByVendor?vendorId=${vendorId}`);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const text = await response.text();
        const invoices = text ? JSON.parse(text) : [];
        
        const select = document.getElementById('returnInvoiceSelect');
        select.innerHTML = '<option value="">-- Choose Invoice --</option>';
        select.disabled = false;
        
        if (!invoices || invoices.length === 0) {
            select.innerHTML = '<option value="">No invoices found for this vendor</option>';
            select.disabled = true;
            document.getElementById('returnProductsSection').style.display = 'none';
            document.getElementById('saveReturnBtn').disabled = true;
            return;
        }
        
        invoices.forEach(invoice => {
            const option = document.createElement('option');
            option.value = invoice.id;
            option.textContent = `Invoice #${invoice.id} - ${invoice.purchaseDate} (${invoice.itemCount} items, ${invoice.totalAmount.toFixed(2)} EGP)`;
            option.dataset.invoice = JSON.stringify(invoice);
            select.appendChild(option);
        });
    } catch (error) {
        console.error('Failed to load invoices:', error);
        console.error('Vendor ID:', vendorId);
        const select = document.getElementById('returnInvoiceSelect');
        select.innerHTML = '<option value="">Error loading invoices</option>';
        select.disabled = true;
        showError('Failed to load invoices for this vendor');
    }
}

function loadInvoiceProducts(invoiceId) {
    const select = document.getElementById('returnInvoiceSelect');
    const selectedOption = select.options[select.selectedIndex];
    const invoice = JSON.parse(selectedOption.dataset.invoice);
    
    const tbody = document.getElementById('returnItemsBody');
    tbody.innerHTML = '';
    
    if (!invoice.items || invoice.items.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center">No items in this invoice</td></tr>';
        document.getElementById('returnProductsSection').style.display = 'block';
        document.getElementById('saveReturnBtn').disabled = true;
        return;
    }
    
    invoice.items.forEach(item => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td class="text-center">
                <input type="checkbox" class="form-check-input return-item-checkbox" data-variant-id="${item.productVariantId}">
            </td>
            <td>${item.productCode} - ${item.productDescription}</td>
            <td><span class="badge bg-secondary">${item.color}</span></td>
            <td><span class="badge bg-info">${item.size}</span></td>
            <td><strong>${item.quantity}</strong></td>
            <td>
                <input type="number" class="form-control form-control-sm return-qty-input" 
                       min="0" max="${item.quantity}" value="0" 
                       data-variant-id="${item.productVariantId}">
            </td>
            <td>
                <input type="number" class="form-control form-control-sm return-price-input" 
                       step="0.01" min="0" value="${item.buyingPrice.toFixed(2)}" readonly
                       data-variant-id="${item.productVariantId}">
            </td>
            <td class="fw-bold text-danger return-subtotal">0.00 EGP</td>
        `;
        tbody.appendChild(row);
        
        // Add event listeners
        const checkbox = row.querySelector('.return-item-checkbox');
        const qtyInput = row.querySelector('.return-qty-input');
        const priceInput = row.querySelector('.return-price-input');
        const subtotalCell = row.querySelector('.return-subtotal');
        
        checkbox.addEventListener('change', function() {
            if (this.checked) {
                qtyInput.value = item.quantity; // Set to max
                qtyInput.disabled = false;
            } else {
                qtyInput.value = 0;
                qtyInput.disabled = true;
            }
            updateReturnRowSubtotal(qtyInput, priceInput, subtotalCell);
        });
        
        qtyInput.addEventListener('input', function() {
            updateReturnRowSubtotal(qtyInput, priceInput, subtotalCell);
        });
        
        qtyInput.disabled = true; // Initially disabled
    });
    
    document.getElementById('returnProductsSection').style.display = 'block';
    document.getElementById('saveReturnBtn').disabled = false;
    calculateReturnTotal();
}

function updateReturnRowSubtotal(qtyInput, priceInput, subtotalCell) {
    const qty = parseInt(qtyInput.value) || 0;
    const price = parseFloat(priceInput.value) || 0;
    const subtotal = qty * price;
    subtotalCell.textContent = subtotal.toFixed(2) + ' EGP';
    calculateReturnTotal();
}

function calculateReturnTotal() {
    const subtotals = document.querySelectorAll('.return-subtotal');
    let total = 0;
    subtotals.forEach(cell => {
        const value = parseFloat(cell.textContent.replace(' EGP', '')) || 0;
        total += value;
    });
    document.getElementById('totalRefund').textContent = total.toFixed(2) + ' EGP';
}

function resetReturnModal() {
    document.getElementById('returnVendorSelect').value = '';
    document.getElementById('returnInvoiceSelect').innerHTML = '<option value="">-- First select a vendor --</option>';
    document.getElementById('returnInvoiceSelect').disabled = true;
    document.getElementById('returnDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('returnReason').value = '';
    document.getElementById('returnItemsBody').innerHTML = '';
    document.getElementById('returnProductsSection').style.display = 'none';
    document.getElementById('totalRefund').textContent = '0.00 EGP';
    document.getElementById('saveReturnBtn').disabled = true;
    document.getElementById('selectAllReturnItems').checked = false;
}

async function saveReturn() {
    const vendorId = parseInt(document.getElementById('returnVendorSelect').value);
    const invoiceId = parseInt(document.getElementById('returnInvoiceSelect').value);
    const returnDate = document.getElementById('returnDate').value;
    const reason = document.getElementById('returnReason').value;

    if (!vendorId || !invoiceId) {
        showError('Please select vendor and invoice');
        return;
    }

    // Collect items
    const items = [];
    const rows = document.querySelectorAll('#returnItemsBody tr');
    
    rows.forEach(row => {
        const checkbox = row.querySelector('.return-item-checkbox');
        const qtyInput = row.querySelector('.return-qty-input');
        const priceInput = row.querySelector('.return-price-input');
        
        if (checkbox && checkbox.checked) {
            const qty = parseInt(qtyInput.value) || 0;
            const price = parseFloat(priceInput.value) || 0;
            const variantId = parseInt(qtyInput.dataset.variantId);
            
            if (qty > 0 && variantId) {
                items.push({
                    productVariantId: variantId,
                    quantity: qty,
                    refundPrice: price
                });
            }
        }
    });

    if (items.length === 0) {
        showError('Please select at least one item with quantity > 0');
        return;
    }

    const total = items.reduce((sum, item) => sum + (item.quantity * item.refundPrice), 0);

    const returnData = {
        purchaseInvoiceId: invoiceId,
        vendorId: vendorId,
        returnDate: returnDate,
        totalRefund: total,
        reason: reason || '',
        items: items
    };

    try {
        const response = await fetch('/Purchases/AddReturn', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(returnData)
        });
        
        if (!response.ok) throw new Error('Failed to save return');
        
        const data = await response.json();
        showSuccess('Return saved successfully!');
        
        const modal = bootstrap.Modal.getInstance(document.getElementById('returnModal'));
        modal.hide();
        
        resetReturnModal();
        loadAllReturns();
    } catch (error) {
        console.error('Error saving return:', error);
        showError(error.message || 'Failed to save return');
    }
}

// === OLD SAVE RETURN FUNCTION (REPLACED ABOVE) ===
function saveReturnOld(e) {
    e.preventDefault();

    const vendorId = parseInt(document.getElementById('returnVendorId').value);
    const returnDate = document.getElementById('returnDate').value;
    const invoiceId = parseInt(document.getElementById('returnInvoiceId').value) || 0;
    const reason = document.getElementById('returnReason').value;

    if (!vendorId) {
        showError('Please select a vendor');
        return;
    }

    const items = [];
    let valid = true;

    const rows = document.querySelectorAll('#returnItemsTable tbody tr');
    rows.forEach(row => {
        const selects = row.querySelectorAll('select');
        const inputs = row.querySelectorAll('input[type="number"]');
        
        const variantId = parseInt(selects[2].value);
        const qty = parseInt(inputs[0].value);
        const price = parseFloat(inputs[1].value);

        if (!variantId || qty <= 0 || price < 0) {
            valid = false;
            return;
        }

        items.push({
            productVariantId: variantId,
            quantity: qty,
            refundPrice: price
        });
    });

    if (!valid || items.length === 0) {
        showError('Please add at least one valid item');
        return;
    }

    const total = items.reduce((sum, item) => sum + (item.quantity * item.refundPrice), 0);

    const returnData = {
        purchaseInvoiceId: invoiceId || null,
        vendorId: vendorId,
        returnDate: returnDate,
        totalRefund: total,
        reason: reason,
        items: items
    };

    fetch('/Purchases/AddReturn', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(returnData)
    })
    .then(response => {
        if (!response.ok) throw new Error('Failed to save return');
        return response.json();
    })
    .then(data => {
        showSuccess('Return saved successfully!');
        const modal = bootstrap.Modal.getInstance(document.getElementById('returnModal'));
        modal.hide();
        clearReturnForm();
        loadAllReturns();
    })
    .catch(error => {
        showError(error.message);
    });
}

function clearReturnForm() {
    document.getElementById('returnForm').reset();
    document.getElementById('returnVendorId').value = '';
    document.querySelector('#returnItemsTable tbody').innerHTML = '';
    document.getElementById('totalRefund').textContent = '0.00 EGP';
}

function loadAllReturns() {
    fetch('/Purchases/GetAllReturns')
        .then(response => response.json())
        .then(data => {
            allReturns = data;
            renderReturnsTable();
        })
        .catch(() => {
            showError('Failed to load returns');
        });
}

function renderReturnsTable() {
    const tbody = document.querySelector('#returnsTable tbody');
    tbody.innerHTML = '';

    if (allReturns.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="text-center empty-state"><i class="fas fa-inbox"></i><h5>No returns yet</h5></td></tr>';
        return;
    }

    allReturns.forEach((ret, index) => {
        const row = document.createElement('tr');
        row.className = 'fade-in';
        row.innerHTML = `
            <td>${index + 1}</td>
            <td>#${ret.id}</td>
            <td>${ret.vendorName || 'N/A'}</td>
            <td>${formatDate(ret.returnDate)}</td>
            <td class="fw-bold text-danger">${ret.totalRefund.toFixed(2)} EGP</td>
            <td>${ret.reason || '-'}</td>
            <td class="text-center">
                <button class="btn btn-sm btn-danger btn-action" onclick="deleteReturn(${ret.id})">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

function deleteReturn(id) {
    ShowModalConfirm('Are you sure you want to delete this return? Stock will be restored.', async () => {
        try {
            const response = await fetch(`/Purchases/DeleteReturn/${id}`, { method: 'DELETE' });
            if (!response.ok) throw new Error('Failed to delete return');
            showSuccess('Return deleted successfully');
            loadAllReturns();
        } catch (err) {
            showError(err.message || 'Failed to delete return');
        }
    }, 'Delete Return', 'Delete', 'Cancel');
}

// ==================== STATISTICS TAB ====================
function loadStatistics() {
    fetch('/Purchases/GetVendorStats')
        .then(async response => {
            if (!response.ok) {
                const text = await response.text();
                throw new Error(text || `HTTP ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            renderStatistics(data);
        })
        .catch((err) => {
            showError('Failed to load statistics: ' + err.message);
        });
}

function renderStatistics(stats) {
    const container = document.getElementById('statsContainer');
    container.innerHTML = '';

    if (stats.length === 0) {
        container.innerHTML = '<div class="col-12 empty-state"><i class="fas fa-chart-bar"></i><h5>No statistics available</h5></div>';
        return;
    }

    stats.forEach(stat => {
        const card = document.createElement('div');
        card.className = 'col-md-6 col-lg-4 mb-4 fade-in';
        card.innerHTML = `
            <div class="card stat-card">
                <div class="card-body">
                    <h5 class="card-title">${stat.vendorName}</h5>
                    <div class="mb-3">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="stat-label">Total Purchased:</span>
                            <span class="badge badge-purchased">${stat.totalPurchased.toFixed(2)} EGP</span>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="stat-label">Total Returned:</span>
                            <span class="badge badge-returned">${stat.totalReturned.toFixed(2)} EGP</span>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="stat-label">Net Purchased:</span>
                            <span class="badge badge-net">${stat.netPurchased.toFixed(2)} EGP</span>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="stat-label">Invoice Count:</span>
                            <span class="badge bg-secondary">${stat.invoiceCount}</span>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <span class="stat-label">Products Sold:</span>
                            <span class="badge badge-sold-count">${stat.productsSoldCount}</span>
                        </div>
                        <div class="d-flex justify-content-between align-items-center">
                            <span class="stat-label">Sales Value:</span>
                            <span class="badge badge-sold-value">${stat.productsSoldValue.toFixed(2)} EGP</span>
                        </div>
                    </div>
                </div>
            </div>
        `;
        container.appendChild(card);
    });
}

// ==================== UTILITIES ====================
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-GB');
}

function showSuccess(message) {
    if (window.ShowToast) {
        ShowToast(message, 'success');
    } else {
        console.log('SUCCESS:', message);
    }
}

function showError(message) {
    if (window.ShowToast) {
        ShowToast(message, 'error');
    } else {
        console.error('ERROR:', message);
    }
}

// Duplicate code cleanup below: remove the legacy jQuery-based implementation completely

// The above removed block was a duplicate jQuery-based implementation kept by mistake. The file now contains a single, consistent vanilla JS implementation.
