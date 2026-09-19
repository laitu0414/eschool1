/**
 * Excel File Import Preview Handler
 * Automatically detects .xlsx file inputs in forms/modals and renders a preview table before import confirmation.
 */
document.addEventListener('DOMContentLoaded', function () {
    initExcelPreview();
});

function initExcelPreview() {
    document.addEventListener('change', function (e) {
        var input = e.target;
        if (!input || input.type !== 'file') return;

        var isExcelInput = (input.accept && input.accept.toLowerCase().includes('.xlsx')) ||
            (input.name && (input.name.toLowerCase().includes('excel') || input.name.toLowerCase().includes('file')));

        var file = input.files && input.files[0];
        if (!file) {
            clearExcelPreview(input);
            return;
        }

        var fileName = file.name.toLowerCase();
        if (!fileName.endsWith('.xlsx') && !fileName.endsWith('.xls')) {
            if (isExcelInput) {
                showExcelPreviewError(input, 'Vui lòng chọn đúng file định dạng Excel (.xlsx hoặc .xls).');
            }
            return;
        }

        renderExcelPreview(input, file);
    });

    // Reset when modal closes
    document.addEventListener('hidden.bs.modal', function (e) {
        var modal = e.target;
        var fileInputs = modal.querySelectorAll('input[type="file"]');
        fileInputs.forEach(function (input) {
            clearExcelPreview(input);
        });
    });
}

function renderExcelPreview(input, file) {
    var form = input.closest('form');
    var modal = input.closest('.modal');
    var modalDialog = modal ? modal.querySelector('.modal-dialog') : null;

    if (modalDialog && !modalDialog.classList.contains('modal-xl')) {
        modalDialog.setAttribute('data-original-class', modalDialog.className);
        modalDialog.classList.remove('modal-sm', 'modal-md', 'modal-lg');
        modalDialog.classList.add('modal-xl');
    }

    // Get or create preview container
    var previewContainer = getOrCreatePreviewContainer(input);
    previewContainer.innerHTML = '<div class="text-center py-3 text-muted"><div class="spinner-border spinner-border-sm text-success me-2" role="status"></div> Đang đọc dữ liệu từ file Excel...</div>';

    var reader = new FileReader();
    reader.onload = function (e) {
        try {
            if (typeof XLSX === 'undefined') {
                showExcelPreviewError(input, 'Thư viện đọc Excel chưa sẵn sàng. Vui lòng thử lại sau giây lát.');
                return;
            }

            var data = new Uint8Array(e.target.result);
            var workbook = XLSX.read(data, { type: 'array' });

            if (!workbook.SheetNames || workbook.SheetNames.length === 0) {
                showExcelPreviewError(input, 'File Excel không có trang tính (sheet) nào.');
                return;
            }

            var firstSheetName = workbook.SheetNames[0];
            var worksheet = workbook.Sheets[firstSheetName];
            var rawRows = XLSX.utils.sheet_to_json(worksheet, { header: 1, defval: '' });

            // Filter empty rows
            var validRows = rawRows.filter(function (row) {
                if (!row || !Array.isArray(row)) return false;
                return row.some(function (cell) {
                    return cell !== null && cell !== undefined && String(cell).trim() !== '';
                });
            });

            if (validRows.length <= 1) {
                showExcelPreviewError(input, 'File Excel không có dữ liệu hoặc chỉ có dòng tiêu đề.');
                return;
            }

            var headerRow = validRows[0];
            var dataRows = validRows.slice(1);

            // Determine max non-empty columns from headers and first 10 rows
            var maxCols = headerRow.length;
            for (var i = 0; i < Math.min(dataRows.length, 10); i++) {
                maxCols = Math.max(maxCols, dataRows[i].length);
            }

            // Build Preview HTML
            var html = '';
            html += '<div class="excel-preview-card mt-3 p-3 bg-white rounded-3 border shadow-sm">';
            html += '  <div class="d-flex flex-wrap justify-content-between align-items-center mb-2 pb-2 border-bottom">';
            html += '    <div class="d-flex align-items-center gap-2">';
            html += '      <span class="badge bg-success-subtle text-success border border-success-subtle px-2 py-1 fs-6"><i class="bi bi-file-earmark-spreadsheet me-1"></i> ' + escapeHtml(file.name) + '</span>';
            html += '      <span class="badge bg-light text-dark border">Sheet: ' + escapeHtml(firstSheetName) + '</span>';
            html += '    </div>';
            html += '    <div class="text-success fw-bold">';
            html += '      <i class="bi bi-check-circle-fill me-1"></i>Tìm thấy ' + dataRows.length + ' dòng dữ liệu';
            html += '    </div>';
            html += '  </div>';

            html += '  <div class="table-responsive rounded border" style="max-height: 280px; overflow-y: auto;">';
            html += '    <table class="table table-bordered table-hover table-sm align-middle small mb-0 text-nowrap">';
            html += '      <thead class="table-light sticky-top" style="z-index: 2; background-color: #f8fafc;">';
            html += '        <tr>';
            html += '          <th class="text-center" style="width: 45px; background: #f1f5f9;">#</th>';
            for (var c = 0; c < maxCols; c++) {
                var colName = headerRow[c] !== undefined && String(headerRow[c]).trim() !== '' ? String(headerRow[c]).trim() : 'Cột ' + (c + 1);
                html += '          <th class="fw-bold text-dark px-2">' + escapeHtml(colName) + '</th>';
            }
            html += '        </tr>';
            html += '      </thead>';
            html += '      <tbody>';

            var displayLimit = Math.min(dataRows.length, 100);
            for (var r = 0; r < displayLimit; r++) {
                var rowData = dataRows[r];
                html += '        <tr>';
                html += '          <td class="text-center text-muted fw-bold bg-light" style="font-size: 0.8rem;">' + (r + 1) + '</td>';
                for (var c = 0; c < maxCols; c++) {
                    var cellVal = rowData && rowData[c] !== undefined ? String(rowData[c]).trim() : '';
                    html += '          <td class="px-2 text-dark">' + (cellVal ? escapeHtml(cellVal) : '<span class="text-muted fst-italic">--</span>') + '</td>';
                }
                html += '        </tr>';
            }
            html += '      </tbody>';
            html += '    </table>';
            html += '  </div>';

            if (dataRows.length > displayLimit) {
                html += '  <div class="text-muted small mt-1 text-end fst-italic">Hiển thị trước 100 dòng đầu tiên của file...</div>';
            }

            html += '  <div class="alert alert-warning py-2 px-3 mt-3 mb-0 d-flex align-items-center rounded-2 small">';
            html += '    <i class="bi bi-exclamation-triangle-fill text-warning me-2 fs-6 flex-shrink-0"></i>';
            html += '    <div>Vui lòng kiểm tra kỹ danh sách dữ liệu xem trước ở trên. Nhấn <strong>Xác nhận nhập dữ liệu</strong> bên dưới để lưu vào hệ thống.</div>';
            html += '  </div>';
            html += '</div>';

            previewContainer.innerHTML = html;

            // Update submit button
            if (form) {
                var submitBtn = form.querySelector('button[type="submit"]');
                if (submitBtn) {
                    if (!submitBtn.getAttribute('data-original-html')) {
                        submitBtn.setAttribute('data-original-html', submitBtn.innerHTML);
                    }
                    submitBtn.innerHTML = '<i class="bi bi-check2-circle me-1"></i> Xác nhận nhập dữ liệu';
                    submitBtn.classList.remove('disabled');
                    submitBtn.removeAttribute('disabled');
                }
            }
        } catch (err) {
            console.error('Excel parse error:', err);
            showExcelPreviewError(input, 'Lỗi khi phân tích file Excel: ' + (err.message || 'File không hợp lệ hoặc bị hỏng.'));
        }
    };

    reader.onerror = function () {
        showExcelPreviewError(input, 'Không thể đọc file đã chọn.');
    };

    reader.readAsArrayBuffer(file);
}

function getOrCreatePreviewContainer(input) {
    var parent = input.closest('.mb-3') || input.parentElement;
    var existing = parent.parentElement.querySelector('.excel-preview-wrapper');
    if (!existing) {
        existing = document.createElement('div');
        existing.className = 'excel-preview-wrapper';
        parent.insertAdjacentElement('afterend', existing);
    }
    return existing;
}

function showExcelPreviewError(input, message) {
    var previewContainer = getOrCreatePreviewContainer(input);
    previewContainer.innerHTML = '<div class="alert alert-danger py-2 px-3 mt-3 mb-0 small d-flex align-items-center"><i class="bi bi-x-circle-fill me-2 fs-6 text-danger flex-shrink-0"></i><div>' + escapeHtml(message) + '</div></div>';
}

function clearExcelPreview(input) {
    if (!input) return;
    var form = input.closest('form');
    var modal = input.closest('.modal');
    var modalDialog = modal ? modal.querySelector('.modal-dialog') : null;

    if (modalDialog && modalDialog.hasAttribute('data-original-class')) {
        modalDialog.className = modalDialog.getAttribute('data-original-class');
        modalDialog.removeAttribute('data-original-class');
    }

    var parent = input.closest('.mb-3') || input.parentElement;
    if (parent && parent.parentElement) {
        var existing = parent.parentElement.querySelector('.excel-preview-wrapper');
        if (existing) {
            existing.remove();
        }
    }

    if (form) {
        var submitBtn = form.querySelector('button[type="submit"]');
        if (submitBtn && submitBtn.hasAttribute('data-original-html')) {
            submitBtn.innerHTML = submitBtn.getAttribute('data-original-html');
            submitBtn.removeAttribute('data-original-html');
        }
    }
}

function escapeHtml(unsafe) {
    if (unsafe === null || unsafe === undefined) return '';
    return String(unsafe)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
