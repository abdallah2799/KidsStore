// Sales Page Logic

let rowIndex = 0;
let currentProductData = {}; // Store product data for each row
let autocompleteTimeout = null;

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    initializeSalesPage();
});

function initializeSalesPage() {
    // Add row button
    document.getElementById('addRowBtn').addEventListener('click', addNewRow);

    // Initialize first row
    initializeRow(0);

    // Amount paid input change
    document.getElementById('amountPaid').addEventListener('input', calculateChange);
    
    // Save Invoice Button
    document.getElementById('saveInvoiceBtn').addEventListener('click', function() {
        if (validateInvoice()) {
            const saveModal = new bootstrap.Modal(document.getElementById('saveConfirmModal'));
            saveModal.show();
        }
    });
    
    // Confirm Save Button
    document.getElementById('confirmSaveBtn').addEventListener('click', async function() {
        const saveModal = bootstrap.Modal.getInstance(document.getElementById('saveConfirmModal'));
        saveModal.hide();
        await saveInvoice();
    });
    
    // Skip Receipt Button
    document.getElementById('skipReceiptBtn').addEventListener('click', function() {
        const receiptModal = bootstrap.Modal.getInstance(document.getElementById('receiptModal'));
        receiptModal.hide();
        resetInvoice();
    });
    
    // Generate Receipt Button
    document.getElementById('generateReceiptBtn').addEventListener('click', async function() {
        const receiptModal = bootstrap.Modal.getInstance(document.getElementById('receiptModal'));
        receiptModal.hide();
        await generateReceipt();
        resetInvoice();
    });
    
    // Cancel Invoice Button
    document.getElementById('cancelInvoiceBtn').addEventListener('click', function() {
        if (confirm('Are you sure you want to cancel this invoice? All data will be lost.')) {
            resetInvoice();
        }
    });
}

function initializeRow(index) {
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (!row) return;

    const productCodeInput = row.querySelector('.product-code-input');
    const colorInput = row.querySelector('.color-input');
    const sizeSelect = row.querySelector('.size-select');
    const quantityInput = row.querySelector('.quantity-input');
    const discountInput = row.querySelector('.discount-input');
    const removeBtn = row.querySelector('.remove-row-btn');

    // Autocomplete on product code input
    productCodeInput.addEventListener('input', function () {
        const query = this.value.trim();
        clearTimeout(autocompleteTimeout);
        
        if (query.length >= 2) {
            autocompleteTimeout = setTimeout(() => {
                showAutocomplete(query, index);
            }, 300); // Debounce 300ms
        } else {
            hideAutocomplete(index);
        }
    });
    
    // Close autocomplete on scroll
    window.addEventListener('scroll', () => hideAutocomplete(index), { passive: true });

    // Product code lookup on blur
    productCodeInput.addEventListener('blur', function () {
        // Delay to allow click on autocomplete item
        setTimeout(() => {
            const code = this.value.trim();
            if (code) {
                fetchProductByCode(code, index);
            }
            hideAutocomplete(index);
        }, 200);
    });

    // Enter key on product code
    productCodeInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            this.blur();
        }
    });

    // Color change updates sizes
    colorInput.addEventListener('change', function () {
        updateSizeOptions(index);
        calculateRowTotal(index);
    });

    // Size change calculates total and validates stock
    sizeSelect.addEventListener('change', function () {
        validateStock(index);
        calculateRowTotal(index);
    });

    // Quantity and discount changes
    quantityInput.addEventListener('input', function () {
        validateStock(index);
        calculateRowTotal(index);
    });

    discountInput.addEventListener('input', function () {
        validateDiscount(index);
        calculateRowTotal(index);
    });

    // Remove row
    removeBtn.addEventListener('click', function () {
        removeRow(index);
    });
}

function addNewRow() {
    rowIndex++;
    const tbody = document.getElementById('salesTableBody');
    
    const newRow = document.createElement('tr');
    newRow.className = 'product-row';
    newRow.setAttribute('data-row-index', rowIndex);
    newRow.innerHTML = `
        <td>
            <input type="text" class="form-control form-control-sm product-code-input" placeholder="Enter code..." autocomplete="off">
        </td>
        <td>
            <input type="text" class="form-control form-control-sm product-name-display" readonly placeholder="Auto-filled">
        </td>
        <td class="position-relative">
            <div class="d-flex align-items-center">
                <div class="color-swatch me-2" style="width:20px;height:20px;border:1px solid #ddd;border-radius:50%;background:#fff;cursor:pointer;"></div>
                <input type="text" class="form-control form-control-sm color-input color-select" placeholder="Color" disabled autocomplete="off">
                <input type="hidden" class="color-hex-input">
            </div>
            <div class="color-dropdown dropdown-menu" style="max-height:300px;overflow-y:auto;"></div>
        </td>
        <td>
            <select class="form-select form-select-sm size-select" disabled>
                <option value="">Select</option>
            </select>
        </td>
        <td>
            <input type="number" class="form-control form-control-sm unit-price-display" readonly placeholder="0.00">
        </td>
        <td>
            <input type="number" class="form-control form-control-sm quantity-input" min="1" value="1">
        </td>
        <td>
            <input type="number" class="form-control form-control-sm discount-input" min="0" max="100" value="0">
        </td>
        <td>
            <input type="number" class="form-control form-control-sm total-display" readonly placeholder="0.00">
        </td>
        <td>
            <button class="btn btn-sm btn-danger remove-row-btn"><i class="fa fa-trash"></i></button>
        </td>
    `;

    tbody.appendChild(newRow);
    initializeRow(rowIndex);
    
    // Focus on new product code input
    newRow.querySelector('.product-code-input').focus();
}

function removeRow(index) {
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (row) {
        delete currentProductData[index];
        row.remove();
        updateSummary();
    }
}

async function fetchProductByCode(code, index) {
    try {
        const response = await fetch(`/Products/GetByCode?code=${encodeURIComponent(code)}`);
        
        if (!response.ok) {
            if (response.status === 404) {
                showProductSearchModal(code, index);
                return;
            }
            throw new Error('Failed to fetch product');
        }

        const product = await response.json();
        populateProductRow(product, index);
        
    } catch (error) {
        console.error('Error fetching product:', error);
        ShowToast('Error loading product. Please try again.', 'error');
    }
}

function populateProductRow(product, index) {
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (!row) return;

    // Store product data
    currentProductData[index] = {
        id: product.id,
        code: product.code,
        description: product.description,
        sellingPrice: product.sellingPrice,
        discountLimit: product.discountLimit || 0,
        variants: product.variants || []
    };

    // Populate fields
    row.querySelector('.product-code-input').value = product.code;
    row.querySelector('.product-name-display').value = product.description;
    row.querySelector('.unit-price-display').value = product.sellingPrice.toFixed(2);

    // Get unique colors from variants that have stock > 0
    const variantsInStock = product.variants.filter(v => v.stock > 0);
    const uniqueColors = [...new Set(variantsInStock.map(v => v.color))];
    
    // Store available colors in the row for filtering
    row.setAttribute('data-available-colors', JSON.stringify(uniqueColors));
    
    // Enable the color input
    const colorInput = row.querySelector('.color-input');
    const colorDropdown = row.querySelector('.color-dropdown');
    const colorHexInput = row.querySelector('.color-hex-input');
    const colorSwatch = row.querySelector('.color-swatch');
    
    colorInput.disabled = false;
    colorInput.placeholder = 'Select or type color';
    
    // Setup color autocomplete for this row with filtered colors
    setupColorAutocompleteForSales(
        colorInput,
        colorDropdown,
        colorHexInput,
        colorSwatch,
        uniqueColors,
        index
    );
    
    // Enable remove button
    row.querySelector('.remove-row-btn').disabled = false;
}

function updateSizeOptions(index) {
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (!row) return;

    const productData = currentProductData[index];
    if (!productData) return;

    const selectedColorName = row.querySelector('.color-input').value.trim();
    const selectedColorHex = row.querySelector('.color-hex-input').value;
    const sizeSelect = row.querySelector('.size-select');
    
    sizeSelect.innerHTML = '<option value="">Select</option>';
    
    if (selectedColorHex) {
        // Filter variants by color hex and stock > 0
        const filteredVariants = productData.variants.filter(v => 
            v.color.toLowerCase() === selectedColorHex.toLowerCase() && v.stock > 0
        );
        filteredVariants.forEach(variant => {
            const option = document.createElement('option');
            option.value = variant.id;
            option.textContent = `${variant.size} (Stock: ${variant.stock})`;
            option.setAttribute('data-stock', variant.stock);
            sizeSelect.appendChild(option);
        });
        sizeSelect.disabled = false;
    } else {
        sizeSelect.disabled = true;
    }
}

function validateDiscount(index) {
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (!row) return;

    const productData = currentProductData[index];
    if (!productData) return;

    const discountInput = row.querySelector('.discount-input');
    const discount = parseFloat(discountInput.value) || 0;
    const maxDiscount = productData.discountLimit || 0;

    const warningDiv = document.getElementById('discountWarning');
    
    if (discount > maxDiscount) {
        warningDiv.textContent = `⚠️ Discount exceeds allowed limit of ${maxDiscount}% for this product.`;
        warningDiv.style.display = 'block';
        discountInput.classList.add('border-danger');
    } else {
        warningDiv.style.display = 'none';
        discountInput.classList.remove('border-danger');
    }
}

function validateStock(index) {
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (!row) return;

    const productData = currentProductData[index];
    if (!productData) return;

    const sizeSelect = row.querySelector('.size-select');
    const quantityInput = row.querySelector('.quantity-input');
    const selectedVariantId = parseInt(sizeSelect.value);
    const requestedQuantity = parseInt(quantityInput.value) || 0;

    if (!selectedVariantId || requestedQuantity === 0) {
        quantityInput.classList.remove('border-danger');
        quantityInput.title = '';
        return;
    }

    // Find the selected variant's stock
    const selectedVariant = productData.variants.find(v => v.id === selectedVariantId);
    
    if (!selectedVariant) return;

    const availableStock = selectedVariant.stock;

    if (requestedQuantity > availableStock) {
        quantityInput.classList.add('border-danger');
        quantityInput.title = `Only ${availableStock} units available in stock`;
        ShowToast(`Warning: Only ${availableStock} units available for this variant`, 'warning');
        
        // Optionally, limit the quantity to available stock
        quantityInput.value = availableStock;
    } else {
        quantityInput.classList.remove('border-danger');
        quantityInput.title = '';
    }
}

function calculateRowTotal(index) {
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (!row) return;

    const productData = currentProductData[index];
    if (!productData) return;

    const unitPrice = parseFloat(row.querySelector('.unit-price-display').value) || 0;
    const quantity = parseInt(row.querySelector('.quantity-input').value) || 0;
    const discount = parseFloat(row.querySelector('.discount-input').value) || 0;

    const subtotal = unitPrice * quantity;
    const discountAmount = subtotal * (discount / 100);
    const total = subtotal - discountAmount;

    row.querySelector('.total-display').value = total.toFixed(2);

    updateSummary();
}

function updateSummary() {
    let subtotal = 0;
    let totalDiscount = 0;

    document.querySelectorAll('.product-row').forEach(row => {
        const unitPrice = parseFloat(row.querySelector('.unit-price-display').value) || 0;
        const quantity = parseInt(row.querySelector('.quantity-input').value) || 0;
        const discount = parseFloat(row.querySelector('.discount-input').value) || 0;
        
        const rowSubtotal = unitPrice * quantity;
        const rowDiscount = rowSubtotal * (discount / 100);
        
        subtotal += rowSubtotal;
        totalDiscount += rowDiscount;
    });

    const netTotal = subtotal - totalDiscount;

    document.getElementById('subtotal').textContent = subtotal.toFixed(2) + ' EGP';
    document.getElementById('totalDiscount').textContent = totalDiscount.toFixed(2) + ' EGP';
    document.getElementById('netTotal').textContent = netTotal.toFixed(2) + ' EGP';

    calculateChange();
}

function calculateChange() {
    const netTotalText = document.getElementById('netTotal').textContent.replace(' EGP', '');
    const netTotal = parseFloat(netTotalText) || 0;
    const amountPaid = parseFloat(document.getElementById('amountPaid').value) || 0;
    const change = amountPaid - netTotal;

    document.getElementById('change').textContent = (change >= 0 ? change.toFixed(2) : '0.00') + ' EGP';
}

function showProductSearchModal(searchTerm, index) {
    // TODO: Implement product search modal
    ShowToast(`Product with code "${searchTerm}" not found. Search modal coming soon.`, 'warning');
}

// Autocomplete functionality
async function showAutocomplete(query, index) {
    try {
        const response = await fetch(`/Products/SearchProducts?query=${encodeURIComponent(query)}`);
        if (!response.ok) return;
        
        const products = await response.json();
        
        if (products.length === 0) {
            hideAutocomplete(index);
            return;
        }
        
        const row = document.querySelector(`tr[data-row-index="${index}"]`);
        const productCodeInput = row.querySelector('.product-code-input');
        
        // Remove existing autocomplete
        let existingList = row.querySelector('.autocomplete-list');
        if (existingList) {
            existingList.remove();
        }
        
        // Create autocomplete list
        const autocompleteList = document.createElement('div');
        autocompleteList.className = 'autocomplete-list';
        autocompleteList.style.cssText = `
            position: absolute;
            background: white;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            max-height: 250px;
            overflow-y: auto;
            z-index: 9999;
            min-width: 300px;
        `;
        
        products.forEach(product => {
            const item = document.createElement('div');
            item.className = 'autocomplete-item';
            item.style.cssText = `
                padding: 10px 15px;
                cursor: pointer;
                border-bottom: 1px solid #f3f4f6;
                transition: background 0.2s;
            `;
            item.innerHTML = `
                <div style="font-weight: 600; color: #1e293b;">${product.code}</div>
                <div style="font-size: 0.875rem; color: #64748b;">${product.description}</div>
                <div style="font-size: 0.875rem; color: #2563eb; margin-top: 2px;">EGP ${product.sellingPrice.toFixed(2)}</div>
            `;
            
            item.addEventListener('mouseenter', function() {
                this.style.background = '#f8fafc';
            });
            
            item.addEventListener('mouseleave', function() {
                this.style.background = 'white';
            });
            
            item.addEventListener('mousedown', function(e) {
                e.preventDefault();
                productCodeInput.value = product.code;
                hideAutocomplete(index);
                fetchProductByCode(product.code, index);
            });
            
            autocompleteList.appendChild(item);
        });
        
        // Position autocomplete relative to viewport using fixed positioning
        const inputRect = productCodeInput.getBoundingClientRect();
        autocompleteList.style.position = 'fixed';
        autocompleteList.style.top = `${inputRect.bottom + 2}px`;
        autocompleteList.style.left = `${inputRect.left}px`;
        autocompleteList.style.width = `${Math.max(inputRect.width, 300)}px`;
        
        // Store index for cleanup
        autocompleteList.setAttribute('data-row-index', index);
        
        // Append to body to avoid z-index and overflow issues
        document.body.appendChild(autocompleteList);
        
    } catch (error) {
        console.error('Error fetching autocomplete:', error);
    }
}

function hideAutocomplete(index) {
    // Remove from body if it exists
    const existingList = document.querySelector(`.autocomplete-list[data-row-index="${index}"]`);
    if (existingList) {
        existingList.remove();
    }
    
    // Also check in row (legacy)
    const row = document.querySelector(`tr[data-row-index="${index}"]`);
    if (row) {
        const existingList = row.querySelector('.autocomplete-list');
        if (existingList) {
            existingList.remove();
        }
    }
}

// Add color swatches to select dropdown
function addColorSwatchesToSelect(selectElement) {
    // This function is no longer needed as we're using color palette instead
}

// Setup color autocomplete for sales (filtered by available product colors)
function setupColorAutocompleteForSales(input, dropdown, hiddenHex, swatch, availableColors, rowIndex) {
    if (!input || !dropdown || !hiddenHex || !swatch) {
        console.error('Missing elements:', { input, dropdown, hiddenHex, swatch });
        return;
    }

    // Normalize hex values (add # prefix if missing)
    const normalizeHex = (hex) => {
        if (!hex) return '';
        hex = hex.toString().trim();
        return hex.startsWith('#') ? hex : '#' + hex;
    };
    
    // Create color objects from the actual available hex values
    // Generate color names based on the hex value or use generic names
    const filteredColors = availableColors.map((hex, index) => {
        const normalizedHex = normalizeHex(hex);
        // Try to find a matching color name from COLORS array
        const allColors = (typeof getAllColors === 'function') ? getAllColors() : [];
        const matchingColor = allColors.find(c => 
            c.hex.toLowerCase() === normalizedHex.toLowerCase()
        );
        
        return {
            name: matchingColor ? matchingColor.name : `Color ${index + 1}`,
            hex: normalizedHex
        };
    });

    // Click swatch to focus input
    swatch.addEventListener('click', function(e) {
        e.stopPropagation();
        input.focus();
    });

    // Show palette on focus
    input.addEventListener('focus', function () {
        showColorPaletteForSales(dropdown, input, hiddenHex, swatch, filteredColors, rowIndex);
    });

    // Filter palette on input
    input.addEventListener('input', function () {
        const query = this.value.toLowerCase().trim();
        
        // Update swatch color as user types
        const colorName = this.value.trim();
        const hex = (typeof getColorHexFromName === 'function') ? getColorHexFromName(colorName) : '';
        if (hex) {
            swatch.style.backgroundColor = hex;
            hiddenHex.value = hex;
        } else {
            swatch.style.backgroundColor = '#fff';
            hiddenHex.value = '';
        }
        
        if (query.length < 1) {
            showColorPaletteForSales(dropdown, input, hiddenHex, swatch, filteredColors, rowIndex);
            return;
        }

        const filtered = filteredColors.filter(c =>
            c.name.toLowerCase().includes(query)
        );

        if (filtered.length === 0) {
            dropdown.classList.remove('show');
            dropdown.innerHTML = '<div class="p-2 text-muted text-center">No matching colors for this product</div>';
            dropdown.classList.add('show');
            return;
        }

        renderColorPaletteForSales(dropdown, filtered, input, hiddenHex, swatch, rowIndex);
    });

    // Update swatch on change
    input.addEventListener('change', function() {
        const colorName = this.value.trim();
        const hex = (typeof getColorHexFromName === 'function') ? getColorHexFromName(colorName) : '';
        if (hex) {
            swatch.style.backgroundColor = hex;
            hiddenHex.value = hex;
        } else {
            swatch.style.backgroundColor = '#fff';
            hiddenHex.value = '';
        }
    });

    // Close on click outside
    document.addEventListener('click', function (e) {
        if (!e.target.closest(`tr[data-row-index="${rowIndex}"]`)) {
            dropdown.classList.remove('show');
        }
    });
}

// Show color palette for sales
function showColorPaletteForSales(dropdown, input, hiddenHex, swatch, colors, rowIndex) {
    renderColorPaletteForSales(dropdown, colors, input, hiddenHex, swatch, rowIndex);
}

// Render color palette for sales
function renderColorPaletteForSales(dropdown, colors, input, hiddenHex, swatch, rowIndex) {
    dropdown.innerHTML = '';
    dropdown.classList.add('show');
    
    // Add hint
    const hint = document.createElement('div');
    hint.className = 'p-2 text-muted small border-bottom';
    hint.innerHTML = '<i class="fas fa-palette me-1"></i> Available colors for this product';
    dropdown.appendChild(hint);
    
    // Create palette grid
    const paletteGrid = document.createElement('div');
    paletteGrid.className = 'color-palette-grid';
    paletteGrid.style.cssText = `
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(40px, 1fr));
        gap: 8px;
        padding: 10px;
    `;
    
    colors.forEach(color => {
        const colorItem = document.createElement('div');
        colorItem.className = 'color-palette-item';
        colorItem.title = color.name;
        colorItem.style.cssText = `
            width: 40px;
            height: 40px;
            border-radius: 50%;
            cursor: pointer;
            border: 2px solid #e5e7eb;
            transition: all 0.2s;
            position: relative;
            background-color: ${color.hex};
        `;
        
        // Hover effect
        colorItem.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.1)';
            this.style.borderColor = '#2563eb';
        });
        
        colorItem.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
            this.style.borderColor = '#e5e7eb';
        });
        
        colorItem.addEventListener('click', function () {
            input.value = color.name;
            hiddenHex.value = color.hex;
            swatch.style.backgroundColor = color.hex;
            dropdown.classList.remove('show');
            
            // Trigger change event
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
        
        paletteGrid.appendChild(colorItem);
    });
    
    dropdown.appendChild(paletteGrid);
}

// Validate Invoice
function validateInvoice() {
    const rows = document.querySelectorAll('#salesTableBody .product-row');
    let hasValidProducts = false;
    
    for (let row of rows) {
        const productCode = row.querySelector('.product-code-input')?.value.trim();
        const size = row.querySelector('.size-select')?.value;
        
        if (productCode && size) {
            hasValidProducts = true;
            break;
        }
    }
    
    if (!hasValidProducts) {
        ShowToast('Please add at least one product to the invoice', 'warning');
        return false;
    }
    
    const netTotal = parseFloat(document.getElementById('netTotal').textContent.replace(' EGP', '')) || 0;
    if (netTotal <= 0) {
        ShowToast('Invoice total must be greater than zero', 'warning');
        return false;
    }
    
    return true;
}

// Save Invoice
let savedInvoiceId = null;

async function saveInvoice() {
    try {
        // Collect invoice data
        const items = [];
        const rows = document.querySelectorAll('#salesTableBody .product-row');
        
        rows.forEach(row => {
            const productCode = row.querySelector('.product-code-input')?.value.trim();
            const sizeSelect = row.querySelector('.size-select');
            const quantityInput = row.querySelector('.quantity-input');
            const discountInput = row.querySelector('.discount-input');
            const unitPrice = row.querySelector('.unit-price-display')?.value;
            
            if (productCode && sizeSelect.value) {
                items.push({
                    ProductVariantId: parseInt(sizeSelect.value),
                    Quantity: parseInt(quantityInput.value) || 1,
                    SellingPrice: parseFloat(unitPrice) || 0,
                    DiscountValue: parseFloat(discountInput.value) || 0
                });
            }
        });
        
        if (items.length === 0) {
            ShowToast('No items to save', 'warning');
            return;
        }
        
        const paymentMethod = document.getElementById('paymentMethod').value;
        const customerName = document.getElementById('customerInput').value.trim();
        const netTotal = parseFloat(document.getElementById('netTotal').textContent.replace(' EGP', '')) || 0;
        
        const invoiceData = {
            SellerId: 1, // This should be the current user ID from session
            PaymentMethod: paymentMethod,
            CustomerName: customerName || null,
            TotalAmount: netTotal,
            Items: items
        };
        
        const response = await fetch('/Sales/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(invoiceData)
        });
        
        if (response.ok) {
            const result = await response.json();
            savedInvoiceId = result.id || result.invoiceId;
            ShowToast('Invoice saved successfully!', 'success');
            
            // Show receipt modal
            const receiptModal = new bootstrap.Modal(document.getElementById('receiptModal'));
            receiptModal.show();
        } else {
            const error = await response.text();
            ShowToast(`Error saving invoice: ${error}`, 'error');
        }
    } catch (error) {
        console.error('Error saving invoice:', error);
        ShowToast('An error occurred while saving the invoice', 'error');
    }
}

// Generate Receipt
async function generateReceipt() {
    if (!savedInvoiceId) {
        ShowToast('No invoice ID available for receipt generation', 'error');
        return;
    }
    
    try {
        // Open receipt in new window for viewing/printing
        const receiptWindow = window.open(`/Sales/ViewReceipt/${savedInvoiceId}`, '_blank', 'width=900,height=700');
        
        if (receiptWindow) {
            ShowToast('Receipt opened in new window', 'success');
        } else {
            ShowToast('Please allow pop-ups to view the receipt', 'warning');
        }
    } catch (error) {
        console.error('Error opening receipt:', error);
        ShowToast('An error occurred while opening the receipt', 'error');
    }
}

// Reset Invoice
function resetInvoice() {
    // Clear all rows except the first one
    const tbody = document.getElementById('salesTableBody');
    const rows = tbody.querySelectorAll('.product-row');
    
    rows.forEach((row, index) => {
        if (index > 0) {
            row.remove();
        }
    });
    
    // Reset first row
    const firstRow = tbody.querySelector('.product-row');
    if (firstRow) {
        firstRow.querySelector('.product-code-input').value = '';
        firstRow.querySelector('.product-name-display').value = '';
        firstRow.querySelector('.color-input').value = '';
        firstRow.querySelector('.color-hex-input').value = '';
        firstRow.querySelector('.size-select').innerHTML = '<option value="">Select</option>';
        firstRow.querySelector('.size-select').disabled = true;
        firstRow.querySelector('.unit-price-display').value = '';
        firstRow.querySelector('.quantity-input').value = '1';
        firstRow.querySelector('.discount-input').value = '0';
        firstRow.querySelector('.total-display').value = '';
        firstRow.querySelector('.remove-row-btn').disabled = true;
        
        const swatch = firstRow.querySelector('.color-swatch');
        if (swatch) swatch.style.backgroundColor = '#fff';
    }
    
    // Reset counters and data
    rowIndex = 0;
    currentProductData = {};
    savedInvoiceId = null;
    
    // Reset summary
    document.getElementById('subtotal').textContent = '0.00 EGP';
    document.getElementById('totalDiscount').textContent = '0.00 EGP';
    document.getElementById('netTotal').textContent = '0.00 EGP';
    document.getElementById('amountPaid').value = '0';
    document.getElementById('change').textContent = '0.00 EGP';
    
    // Reset customer
    document.getElementById('customerInput').value = '';
    document.getElementById('paymentMethod').value = 'Cash';
    
    ShowToast('Invoice reset successfully', 'info');
}
