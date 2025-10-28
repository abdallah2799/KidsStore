// Vendors Page JavaScript
document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('searchInput');
    const tableBody = document.querySelector('#vendorsTable tbody');
    const addBtn = document.getElementById('addVendorBtn');
    const addBtnEmpty = document.getElementById('addVendorBtnEmpty');
    const saveBtn = document.getElementById('saveVendorBtn');
    const modalTitle = document.getElementById('vendorModalLabel');
    const form = document.getElementById('vendorForm');
    const nameInput = document.getElementById('vendorName');
    const vendorsTable = document.getElementById('vendorsTable');
    const noVendorsPlaceholder = document.getElementById('noVendorsPlaceholder');

    // 🔹 Filter table rows
    searchInput.addEventListener('keyup', function () {
        document.querySelectorAll('#vendorsTable tbody tr').forEach(row => {
            row.style.display = row.innerText.toLowerCase().includes(this.value.toLowerCase()) ? '' : 'none';
        });
    });

    // 🔹 Add mode
    [addBtn, addBtnEmpty].forEach(btn => {
        if (btn) {
            btn.addEventListener('click', () => {
                modalTitle.innerHTML = '<i class="fas fa-industry"></i> Add Vendor';
                form.reset();
                saveBtn.innerHTML = '<i class="fas fa-save me-1"></i> Save Vendor';
                document.getElementById('vendorId').value = '';
                nameInput.classList.remove('is-invalid');
            });
        }
    });

    // 🔹 Live name check
    nameInput.addEventListener('input', async function () {
        const id = document.getElementById('vendorId').value;
        if (id) return;
        const name = this.value.trim();
        if (name.length < 2) return;

        try {
            const res = await fetch(`/Vendors/CheckName?name=${encodeURIComponent(name)}`);
            const data = await res.json();
            if (data.exists) {
                this.classList.add('is-invalid');
                ShowToast('Vendor name already exists!', 'error', 2500, 'top-right');
            } else {
                this.classList.remove('is-invalid');
            }
        } catch {
            ShowToast('Error checking name availability.', 'warning', 2500, 'top-right');
        }
    });

    // 🔹 Edit mode
    function bindEditButtons() {
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const row = e.target.closest('tr');
                const cells = row.querySelectorAll('td');

                document.getElementById('vendorId').value = cells[0].textContent.trim();
                document.getElementById('vendorName').value = cells[1].textContent.trim();
                document.getElementById('vendorCode').value = cells[2].textContent.trim();
                document.getElementById('vendorContact').value = cells[3].textContent.trim();
                document.getElementById('vendorAddress').value = cells[4].textContent.trim();
                document.getElementById('vendorNotes').value = cells[5].textContent.trim();

                modalTitle.innerHTML = '<i class="fas fa-edit"></i> Edit Vendor';
                saveBtn.innerHTML = '<i class="fas fa-save me-1"></i> Update Vendor';
                nameInput.classList.remove('is-invalid');
            });
        });
    }
    bindEditButtons();

    // 🔹 Save vendor
    saveBtn.addEventListener('click', async () => {
        const id = document.getElementById('vendorId').value;
        const vendor = {
            Id: id || 0,
            Name: document.getElementById('vendorName').value.trim(),
            CodePrefix: document.getElementById('vendorCode').value.trim(),
            ContactInfo: document.getElementById('vendorContact').value.trim(),
            Address: document.getElementById('vendorAddress').value.trim(),
            Notes: document.getElementById('vendorNotes').value.trim()
        };

        if (!vendor.Name) {
            ShowToast('Vendor name is required.', 'warning');
            return;
        }

        const url = id ? '/Vendors/Update' : '/Vendors/Add';
        saveBtn.disabled = true;

        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(vendor)
            });

            if (!res.ok) throw new Error('Network error');
            const updatedVendor = await res.json();

            // Hide placeholder if adding first vendor
            if (!id && noVendorsPlaceholder) {
                noVendorsPlaceholder.classList.add('d-none');
                vendorsTable.classList.remove('d-none');
            }

            if (id) {
                // Update existing row
                const row = [...tableBody.rows].find(r => r.cells[0].textContent == id);
                if (row) {
                    row.cells[1].textContent = updatedVendor.name;
                    row.cells[2].textContent = updatedVendor.codePrefix;
                    row.cells[3].textContent = updatedVendor.contactInfo;
                    row.cells[4].textContent = updatedVendor.address;
                    row.cells[5].textContent = updatedVendor.notes;
                }
                ShowToast(`Vendor #${id} updated successfully!`, 'success');
            } else {
                // Add new row
                const newRow = tableBody.insertRow();
                newRow.innerHTML = `
                    <td>${updatedVendor.id}</td>
                    <td>${updatedVendor.name}</td>
                    <td>${updatedVendor.codePrefix}</td>
                    <td>${updatedVendor.contactInfo || ''}</td>
                    <td>${updatedVendor.address || ''}</td>
                    <td>${updatedVendor.notes || ''}</td>
                    <td class="text-center">
                        <button class="btn btn-sm btn-outline-primary me-1 edit-btn" data-bs-toggle="modal" data-bs-target="#vendorModal">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger delete-btn">
                            <i class="fas fa-trash"></i>
                        </button>
                    </td>`;
                bindEditButtons();
                ShowToast(`Vendor #${updatedVendor.id} added successfully!`, 'success');
            }

            // ✅ Close modal safely
            const modalEl = document.getElementById('vendorModal');
            const modalInstance = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            modalInstance.hide();

        } catch (err) {
            console.error(err);
            ShowToast('Something went wrong while saving.', 'error');
        } finally {
            saveBtn.disabled = false;
        }
    });

    // 🔹 Delete Vendor
    tableBody.addEventListener('click', async (e) => {
        if (e.target.closest('.delete-btn')) {
            const row = e.target.closest('tr');
            const id = row.cells[0].textContent.trim();

            ShowModalConfirm(
                `Are you sure you want to delete vendor <strong>#${id}</strong>?`,
                async () => {
                    try {
                        const res = await fetch(`/Vendors/Delete/${id}`, { method: 'DELETE' });
                        if (res.ok) {
                            row.remove();
                            ShowModalAlert(`Vendor <strong>#${id}</strong> deleted successfully!`, 'Success', 'success');

                            // Show placeholder if table empty
                            if (tableBody.rows.length === 0) {
                                noVendorsPlaceholder.classList.remove('d-none');
                                vendorsTable.classList.add('d-none');
                            }
                        } else {
                            ShowModalAlert(`Failed to delete vendor <strong>#${id}</strong>.`, 'Error', 'error');
                        }
                    } catch {
                        ShowModalAlert('Could not connect to server.', 'Connection Error', 'error');
                    }
                },
                "Delete Vendor",
                "Delete",
                "Cancel"
            );
        }
    });
});
