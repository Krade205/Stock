// Hàm hiển thị ảnh
function showPreview(file) {
    if (file) {
        var reader = new FileReader();
        reader.onload = function (e) {
            var img = document.getElementById('previewImg');
            img.src = e.target.result;
            img.style.display = 'block';
            document.getElementById('uploadText').style.display = 'none';
        }
        reader.readAsDataURL(file);
    }
}

// 1. Khi chọn bằng click
function previewImage(input) {
    if (input.files && input.files[0]) {
        showPreview(input.files[0]);
    }
}

// 2. Xử lý Kéo & Thả (Drag & Drop)
var dropZone = document.getElementById('drop-zone');
var fileInput = document.getElementById('imageFile'); // Lấy thẻ input thật

// Hiệu ứng khi kéo vào
dropZone.addEventListener('dragover', function (e) {
    e.preventDefault();
    this.style.backgroundColor = '#e2e6ea';
    this.style.borderColor = '#0d6efd';
});

// Hiệu ứng khi kéo ra
dropZone.addEventListener('dragleave', function (e) {
    e.preventDefault();
    this.style.backgroundColor = '#f8f9fa';
    this.style.borderColor = '#dee2e6';
});

// KHI THẢ ẢNH VÀO (QUAN TRỌNG NHẤT)
dropZone.addEventListener('drop', function (e) {
    e.preventDefault();
    this.style.backgroundColor = '#f8f9fa';

    // Lấy file từ hành động thả
    var droppedFiles = e.dataTransfer.files;

    if (droppedFiles.length > 0) {
        // GÁN FILE VÀO INPUT THẬT (Để Server nhận được)
        fileInput.files = droppedFiles;

        // Hiển thị ảnh lên
        showPreview(droppedFiles[0]);
    }
});