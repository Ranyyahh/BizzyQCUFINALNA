(function () {
    var imageInput = document.getElementById('productImage');
    var preview = document.getElementById('imagePreview');
    var imageLabel = document.querySelector('.add-product-image-icon');
    var saveBtn = document.getElementById('saveProductBtn');

    var processedImageFile = null;

    function showToast(message, isError) {
        var existing = document.getElementById('addProductToast');
        if (existing) existing.remove();

        var toast = document.createElement('div');
        toast.id = 'addProductToast';
        toast.className = 'add-product-toast ' + (isError ? 'is-error' : 'is-success');
        toast.textContent = message;

        document.body.appendChild(toast);
        setTimeout(function () { toast.classList.add('show'); }, 10);
        setTimeout(function () {
            toast.classList.remove('show');
            setTimeout(function () {
                if (toast.parentNode) toast.parentNode.removeChild(toast);
            }, 220);
        }, 3300);
    }

    function setSavingState(isSaving) {
        if (!saveBtn) return;
        saveBtn.disabled = isSaving;
        saveBtn.textContent = isSaving ? 'Saving...' : 'Save Product';
        saveBtn.classList.toggle('is-loading', isSaving);
    }

    function loadImage(file) {
        return new Promise(function (resolve, reject) {
            var reader = new FileReader();
            reader.onload = function (ev) {
                var img = new Image();
                img.onload = function () { resolve(img); };
                img.onerror = function () { reject(new Error('Unsupported image format.')); };
                img.src = ev.target.result;
            };
            reader.onerror = function () { reject(new Error('Could not read selected image.')); };
            reader.readAsDataURL(file);
        });
    }

    function canvasToBlob(canvas, quality) {
        return new Promise(function (resolve, reject) {
            canvas.toBlob(function (blob) {
                if (!blob) return reject(new Error('Could not process image.'));
                resolve(blob);
            }, 'image/jpeg', quality);
        });
    }

    async function normalizeImage(file) {
        var img = await loadImage(file);
        var maxDim = 1800;
        var width = img.naturalWidth || img.width;
        var height = img.naturalHeight || img.height;
        var scale = Math.min(1, maxDim / Math.max(width, height));

        var targetW = Math.max(1, Math.round(width * scale));
        var targetH = Math.max(1, Math.round(height * scale));

        var canvas = document.createElement('canvas');
        canvas.width = targetW;
        canvas.height = targetH;

        var ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0, targetW, targetH);

        var blob = await canvasToBlob(canvas, 0.86);
        return new File([blob], 'product-upload.jpg', { type: 'image/jpeg' });
    }

    if (imageInput) {
        imageInput.addEventListener('change', async function (e) {
            var file = e.target.files && e.target.files[0] ? e.target.files[0] : null;
            processedImageFile = null;

            if (!file) {
                if (preview) preview.style.display = 'none';
                if (imageLabel) imageLabel.style.display = 'block';
                return;
            }

            if (file.size > (15 * 1024 * 1024)) {
                showToast('Image must be 15MB or below.', true);
                imageInput.value = '';
                if (preview) preview.style.display = 'none';
                if (imageLabel) imageLabel.style.display = 'block';
                return;
            }

            try {
                processedImageFile = await normalizeImage(file);
                var reader = new FileReader();
                reader.onload = function (event) {
                    if (preview) {
                        preview.src = event.target.result;
                        preview.style.display = 'block';
                    }
                    if (imageLabel) imageLabel.style.display = 'none';
                };
                reader.readAsDataURL(processedImageFile);
            } catch (err) {
                showToast(err.message || 'Could not process this image.', true);
                imageInput.value = '';
                if (preview) preview.style.display = 'none';
                if (imageLabel) imageLabel.style.display = 'block';
            }
        });
    }

    if (!saveBtn) return;

    saveBtn.addEventListener('click', async function () {
        var name = document.getElementById('productName').value.trim();
        var priceRaw = document.getElementById('productPrice').value;
        var category = document.getElementById('productCategory').value;
        var preparationTimeRaw = document.getElementById('preparationTime').value.trim();
        var description = document.getElementById('productDescription').value.trim();

        if (!name) return showToast('Please enter product name.', true);

        var price = Number(priceRaw);
        if (!priceRaw || isNaN(price) || price <= 0) return showToast('Please enter a valid price greater than 0.', true);

        if (!category) return showToast('Please select a category.', true);

        if (!preparationTimeRaw) return showToast('Please enter preparation time in minutes.', true);

        var prepMinutes = Number(preparationTimeRaw);
        if (!Number.isInteger(prepMinutes)) return showToast('Preparation time must be whole minutes only.', true);

        if (prepMinutes < 1 || prepMinutes > 60) return showToast('Preparation time must be between 1 and 60 minutes.', true);

        var formData = new FormData();
        formData.append('name', name);
        formData.append('price', price.toFixed(2));
        formData.append('category', category);
        formData.append('preparationTime', prepMinutes.toString());
        formData.append('description', description);
        if (processedImageFile) {
            formData.append('productImage', processedImageFile);
        }

        setSavingState(true);

        try {
            var response = await fetch('/AddProduct/AddProductAjax', {
                method: 'POST',
                body: formData,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) {
                setSavingState(false);
                return showToast('Server error while saving product.', true);
            }

            var data = await response.json();
            if (data && data.success) {
                showToast(data.message || 'Product submitted for approval!', false);
                setTimeout(function () {
                    window.location.href = '/EnterpriseDashboard/EnterpriseDashboard';
                }, 900);
                return;
            }

            setSavingState(false);
            showToast((data && data.message) ? data.message : 'Failed to submit product.', true);
        } catch (error) {
            setSavingState(false);
            showToast('Failed to add product. Please try again.', true);
        }
    });
})();
