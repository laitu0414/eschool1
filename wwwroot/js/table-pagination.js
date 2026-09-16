(function () {
    const PAGE_SIZES = [10, 20, 50, 100];
    const DEFAULT_PAGE_SIZE = 10;

    function getBody(table) {
        let body = table.tBodies[0];
        if (!body) {
            body = document.createElement("tbody");
            table.appendChild(body);
        }
        return body;
    }

    function isTimetableOrSchedule(table) {
        const headers = Array.from(table.querySelectorAll("th"));
        return headers.some(th => {
            const text = th.textContent.trim();
            return text.includes("Thứ 2") || text.includes("Thứ 3") || text === "Tiết";
        });
    }

    function shouldPaginate(table) {
        if (table.dataset.noPagination === "true") return false;
        if (table.closest("[data-no-pagination='true']")) return false;
        if (table.closest(".app-pagination")) return false;
        if (table.closest("td, th")) return false;
        if (table.closest(".modal")) return false;
        if (isTimetableOrSchedule(table)) return false;

        // Ensure table has a header structure (data table)
        const hasHeaders = table.tHead || table.querySelector("thead, tr th");
        if (!hasHeaders) return false;

        return true;
    }

    function getTableRows(table) {
        const body = getBody(table);
        let allRows = Array.from(body.rows);
        const colCount = table.querySelectorAll("thead th").length || 6;

        // If tbody is completely empty, insert standard empty state row
        if (allRows.length === 0) {
            const emptyTr = document.createElement("tr");
            emptyTr.className = "app-table-empty-row";
            emptyTr.innerHTML = `<td colspan="${colCount}" class="app-table-empty-cell">Không có dữ liệu</td>`;
            body.appendChild(emptyTr);
            allRows = [emptyTr];
        }

        // Check if the only row is an empty-state row (single cell with colSpan > 1)
        if (allRows.length === 1) {
            const firstRow = allRows[0];
            const isSingleCellColspan = firstRow.cells.length === 1 && (
                firstRow.cells[0].colSpan > 1 ||
                firstRow.classList.contains("app-table-empty-row") ||
                firstRow.querySelector(".text-muted, .alert, .fs-1, .bi-inbox, .bi-people, .bi-folder-x, .bi-calendar-x, .bi-journal-x, .bi-person-lines-fill, .bi-person-workspace, .bi-person-hearts")
            );
            if (isSingleCellColspan) {
                firstRow.classList.add("app-table-empty-row");
                if (firstRow.cells[0]) {
                    firstRow.cells[0].classList.add("app-table-empty-cell");
                }
                return { dataRows: [], emptyRow: firstRow };
            }
        }

        // Filter out any empty message row
        const dataRows = allRows.filter(row => {
            const isColspanEmpty = row.cells.length === 1 && (row.cells[0].colSpan > 1 || row.classList.contains("app-table-empty-row"));
            return !isColspanEmpty;
        });

        const emptyRow = allRows.length === 1 && dataRows.length === 0 ? allRows[0] : null;
        if (emptyRow) {
            emptyRow.classList.add("app-table-empty-row");
            if (emptyRow.cells[0]) {
                emptyRow.cells[0].classList.add("app-table-empty-cell");
            }
        }

        return { dataRows, emptyRow };
    }

    function visiblePages(currentPage, totalPages) {
        if (totalPages <= 7) {
            return Array.from({ length: totalPages }, (_, i) => i + 1);
        }
        const pages = new Set([
            1,
            totalPages,
            currentPage - 1,
            currentPage,
            currentPage + 1
        ]);
        if (currentPage <= 3) {
            pages.add(2);
            pages.add(3);
            pages.add(4);
        }
        if (currentPage >= totalPages - 2) {
            pages.add(totalPages - 1);
            pages.add(totalPages - 2);
            pages.add(totalPages - 3);
        }
        return Array.from(pages)
            .filter(page => page >= 1 && page <= totalPages)
            .sort((a, b) => a - b);
    }

    function icon(name) {
        return `<i class="bi bi-chevron-${name}" aria-hidden="true"></i>`;
    }

    function shouldSkipCheckboxes(table) {
        if (!table) return true;
        if (table.dataset.noCheckbox === "true" || table.classList.contains("no-checkbox")) return true;
        if (table.closest("[data-no-checkbox='true'], .no-checkbox")) return true;

        // Check if inside student, teacher, parent, or result portal interfaces
        const isPortal = table.closest(".student-app, .teacher-app, .parent-app, .result-app, .student-portal, .teacher-portal, .parent-portal") ||
                         document.querySelector(".teacher-app, .parent-app, .student-app, .result-app") ||
                         document.querySelector(".sidebar-logo small")?.textContent?.includes("Cổng học sinh") ||
                         document.querySelector(".sidebar-logo small")?.textContent?.includes("Cổng phụ huynh") ||
                         document.querySelector(".sidebar .brand small")?.textContent?.includes("Quản lý giáo viên") ||
                         document.querySelector(".sidebar .brand small")?.textContent?.includes("Kết quả học tập");

        const isAdmin = document.querySelector(".academic-app") ||
                        (document.querySelector(".app") && !document.querySelector(".teacher-app, .parent-app, .student-app"));

        if (isAdmin) return false;
        if (isPortal) return true;

        return false;
    }

    function setupBulkDeleteUI(table) {
        if (table.dataset.bulkDeleteSetup === "true") return;
        
        // Skip bulk delete checkboxes on student, teacher, and parent interfaces
        if (shouldSkipCheckboxes(table)) {
            table.dataset.bulkDeleteSetup = "skipped";
            return;
        }

        table.dataset.bulkDeleteSetup = "true";

        // Setup the master checkbox in thead
        const theadRow = table.querySelector("thead tr");
        if (theadRow && !theadRow.querySelector(".app-table-checkbox-col")) {
            const th = document.createElement("th");
            th.className = "app-table-checkbox-col text-center";
            th.style.width = "40px";
            th.innerHTML = `<input type="checkbox" class="app-table-checkbox-all form-check-input" aria-label="Chọn tất cả">`;
            theadRow.insertBefore(th, theadRow.firstChild);
        }

        const masterCheckbox = table.querySelector(".app-table-checkbox-all");

        // Setup the bulk delete button in page tools
        let toolsContainer = null;
        let addBtn = null;
        let deleteBtn = null;

        // Try Strategy 1: Standard .page-card > .page-tools
        const pageCard = table.closest(".page-card");
        if (pageCard) {
            toolsContainer = pageCard.querySelector(".page-tools");
            if (toolsContainer) {
                addBtn = toolsContainer.querySelector(".btn-primary, [data-bs-target*='create']");
                if (addBtn && addBtn.parentElement !== toolsContainer) {
                    toolsContainer = addBtn.parentElement;
                }
            }
        }

        // Try Strategy 2: Look in the broader wrapper for any button containing "Thêm" or a plus icon
        if (!addBtn) {
            const wrapper = table.closest(".student-management-page, .teacher-management-page, .main, .container, body");
            if (wrapper) {
                const allButtons = Array.from(wrapper.querySelectorAll("button, a.btn, .btn"));
                addBtn = allButtons.find(btn => {
                    const text = btn.textContent.toLowerCase();
                    const hasPlus = btn.querySelector(".bi-plus-lg, .bi-plus");
                    return text.includes("thêm") || hasPlus;
                });
                
                if (addBtn) {
                    toolsContainer = addBtn.parentElement;
                }
            }
        }
        
        if (addBtn && toolsContainer) {
            // If the button is directly inside .page-tools, wrap it in a flex container to keep it aligned to the right
            if (toolsContainer.classList.contains("page-tools")) {
                const wrapper = document.createElement("div");
                wrapper.className = "d-flex gap-2 align-items-center ms-auto";
                toolsContainer.insertBefore(wrapper, addBtn);
                wrapper.appendChild(addBtn);
                toolsContainer = wrapper; // Update toolsContainer to be the new wrapper
            }

            deleteBtn = toolsContainer.querySelector(".btn-bulk-delete");
            if (!deleteBtn) {
                deleteBtn = document.createElement("button");
                deleteBtn.className = "btn btn-outline-secondary btn-bulk-delete";
                deleteBtn.disabled = true;
                deleteBtn.innerHTML = `<i class="bi bi-trash"></i> Xóa <span class="bulk-count"></span>`;
                toolsContainer.insertBefore(deleteBtn, addBtn);
            }
        }

        function updateDeleteButtonState() {
            const checkedRows = table.querySelectorAll(".app-table-checkbox-row:checked");
            const count = checkedRows.length;
            
            if (deleteBtn) {
                if (count > 0) {
                    deleteBtn.disabled = false;
                    deleteBtn.className = "btn btn-danger btn-bulk-delete me-2";
                    deleteBtn.innerHTML = `<i class="bi bi-trash"></i> Xóa ( ${count} )`;
                } else {
                    deleteBtn.disabled = true;
                    deleteBtn.className = "btn btn-outline-secondary btn-bulk-delete me-2";
                    deleteBtn.innerHTML = `<i class="bi bi-trash"></i> Xóa <span class="bulk-count"></span>`;
                }
            }

            if (masterCheckbox) {
                const totalCheckboxes = table.querySelectorAll(".app-table-checkbox-row:not(:disabled)");
                masterCheckbox.checked = totalCheckboxes.length > 0 && checkedRows.length === totalCheckboxes.length;
                masterCheckbox.indeterminate = count > 0 && count < totalCheckboxes.length;
            }
        }

        if (masterCheckbox) {
            masterCheckbox.addEventListener("change", (e) => {
                const isChecked = e.target.checked;
                // Only toggle checkboxes that are currently visible in the DOM (current page)
                const visibleCheckboxes = table.querySelectorAll("tbody tr:not([hidden]) .app-table-checkbox-row:not(:disabled)");
                visibleCheckboxes.forEach(cb => cb.checked = isChecked);
                updateDeleteButtonState();
            });
        }

        // Add event listener to the table to handle row checkbox changes dynamically
        table.addEventListener("change", (e) => {
            if (e.target.classList.contains("app-table-checkbox-row")) {
                updateDeleteButtonState();
            }
        });

        table.updateDeleteButtonState = updateDeleteButtonState;
    }

    function ensureRowCheckboxes(table, dataRows) {
        if (table.dataset.bulkDeleteSetup !== "true") return;
        
        dataRows.forEach(row => {
            if (!row.querySelector(".app-table-checkbox-cell")) {
                const td = document.createElement("td");
                td.className = "app-table-checkbox-cell text-center";
                td.style.width = "40px";
                td.innerHTML = `<input type="checkbox" class="app-table-checkbox-row form-check-input" value="${row.dataset.id || ''}">`;
                row.insertBefore(td, row.firstChild);
            }
        });
    }

    function buildPager(table) {
        // Tag table with master data table styling
        table.classList.add("app-data-table");
        
        // Initialize bulk delete UI (injects the master checkbox and delete button)
        setupBulkDeleteUI(table);

        // Ensure table is inside the fixed-size scrollable wrapper
        let scrollWrap = table.closest(".app-table-wrapper");
        if (!scrollWrap) {
            const existingWrap = table.closest(".table-responsive, .teacher-admin-table-wrap, .student-admin-table-wrap");
            if (existingWrap) {
                existingWrap.classList.add("app-table-wrapper");
                scrollWrap = existingWrap;
            } else {
                scrollWrap = document.createElement("div");
                scrollWrap.className = "app-table-wrapper table-responsive";
                table.parentNode.insertBefore(scrollWrap, table);
                scrollWrap.appendChild(table);
            }
        }

        // Remove any existing pagination element right next to container
        if (scrollWrap.nextElementSibling && scrollWrap.nextElementSibling.classList.contains("app-pagination")) {
            scrollWrap.nextElementSibling.remove();
        }

        const state = {
            page: 1,
            pageSize: DEFAULT_PAGE_SIZE,
            totalRows: 0,
            totalPages: 1
        };

        const wrapper = document.createElement("div");
        wrapper.className = "app-pagination";
        wrapper.innerHTML = `
            <div class="app-pagination-total">Tổng số: 0 bản ghi</div>
            <nav class="app-pagination-pages" aria-label="Phân trang dữ liệu"></nav>
            <div class="app-page-size">
                <span>Số hàng mỗi trang</span>
                <div class="app-page-size-select">
                    <button type="button" class="app-page-size-button" aria-haspopup="listbox" aria-expanded="false">
                        <span>${state.pageSize}</span>
                        ${icon("down")}
                    </button>
                    <div class="app-page-size-menu" role="listbox"></div>
                </div>
            </div>
        `;

        const totalEl = wrapper.querySelector(".app-pagination-total");
        const pagesEl = wrapper.querySelector(".app-pagination-pages");
        const sizeButton = wrapper.querySelector(".app-page-size-button");
        const sizeButtonText = sizeButton.querySelector("span");
        const sizeMenu = wrapper.querySelector(".app-page-size-menu");

        PAGE_SIZES.forEach(size => {
            const option = document.createElement("button");
            option.type = "button";
            option.className = "app-page-size-option";
            option.dataset.size = size.toString();
            option.setAttribute("role", "option");
            option.innerHTML = `<i class="bi bi-check-lg" aria-hidden="true"></i><span>${size}</span>`;
            option.addEventListener("click", () => {
                state.pageSize = size;
                state.page = 1;
                sizeButtonText.textContent = size;
                sizeButton.setAttribute("aria-expanded", "false");
                sizeMenu.classList.remove("show");
                render();
            });
            sizeMenu.appendChild(option);
        });

        sizeButton.addEventListener("click", event => {
            event.stopPropagation();
            const opened = sizeMenu.classList.toggle("show");
            sizeButton.setAttribute("aria-expanded", opened ? "true" : "false");
        });

        document.addEventListener("click", event => {
            if (!wrapper.contains(event.target)) {
                sizeMenu.classList.remove("show");
                sizeButton.setAttribute("aria-expanded", "false");
            }
        });

        function goToPage(page) {
            state.page = Math.min(Math.max(page, 1), state.totalPages);
            render();
            // Scroll table to top when page changes
            scrollWrap.scrollTop = 0;
        }

        function pageButton(label, page, className, disabled, active) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = className;
            button.disabled = disabled;
            button.innerHTML = label;
            if (active) button.setAttribute("aria-current", "page");
            if (!disabled && page) button.addEventListener("click", () => goToPage(page));
            return button;
        }

        function renderPages() {
            pagesEl.innerHTML = "";
            const isPrevDisabled = state.page <= 1;
            pagesEl.appendChild(pageButton(`${icon("left")}<span>Trước</span>`, state.page - 1, "app-page-nav", isPrevDisabled));

            let previous = 0;
            visiblePages(state.page, state.totalPages).forEach(page => {
                if (previous && page - previous > 1) {
                    const ellipsis = document.createElement("span");
                    ellipsis.className = "app-page-ellipsis";
                    ellipsis.textContent = "...";
                    pagesEl.appendChild(ellipsis);
                }
                pagesEl.appendChild(pageButton(page.toString(), page, "app-page-number", false, page === state.page));
                previous = page;
            });

            const isNextDisabled = state.page >= state.totalPages;
            pagesEl.appendChild(pageButton(`<span>Sau</span>${icon("right")}`, state.page + 1, "app-page-nav", isNextDisabled));
        }

        function renderSizeOptions() {
            Array.from(sizeMenu.children).forEach(option => {
                const selected = Number(option.dataset.size) === state.pageSize;
                option.classList.toggle("selected", selected);
                option.setAttribute("aria-selected", selected ? "true" : "false");
            });
        }

        function render() {
            const { dataRows, emptyRow } = getTableRows(table);
            
            // Inject row checkboxes for any new data rows
            ensureRowCheckboxes(table, dataRows);
            
            state.totalRows = dataRows.length;
            state.totalPages = Math.max(1, Math.ceil(state.totalRows / state.pageSize));
            state.page = Math.min(Math.max(state.page, 1), state.totalPages);

            if (totalEl) {
                totalEl.textContent = `Tổng số: ${state.totalRows} bản ghi`;
            }

            const start = (state.page - 1) * state.pageSize;
            const end = start + state.pageSize;

            if (emptyRow) {
                emptyRow.hidden = false;
            }

            dataRows.forEach((row, index) => {
                row.hidden = index < start || index >= end;
            });

            renderPages();
            renderSizeOptions();
            
            // Update master checkbox based on current view
            if (typeof table.updateDeleteButtonState === 'function') {
                table.updateDeleteButtonState();
            }
        }

        scrollWrap.insertAdjacentElement("afterend", wrapper);
        render();

        // Listen for changes in tbody rows
        const body = getBody(table);
        const observer = new MutationObserver(() => {
            render();
        });
        observer.observe(body, { childList: true });
    }

    function initPagination() {
        document.querySelectorAll("table").forEach(table => {
            if (table.dataset.paginationReady === "true") return;
            if (!shouldPaginate(table)) return;

            table.dataset.paginationReady = "true";
            buildPager(table);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initPagination);
    } else {
        initPagination();
    }

    window.addEventListener("load", initPagination);

    const observer = new MutationObserver(() => initPagination());
    observer.observe(document.documentElement, { childList: true, subtree: true });
})();
