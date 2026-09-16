/**
 * Universal Table Column Manager & Full-Height Column Resizer
 * Automatically attaches to all tables across all pages in eSchool
 */
(function () {
    'use strict';

    // Global guide line element that runs all the way down the table
    let guideLine = document.getElementById('table-col-resize-guide-line');
    if (!guideLine) {
        guideLine = document.createElement('div');
        guideLine.id = 'table-col-resize-guide-line';
        guideLine.className = 'table-col-resize-guide-line';
        document.body.appendChild(guideLine);
    }

    function getPageKey(tbl) {
        const tableId = tbl.id || tbl.getAttribute('data-table-id') || 'tbl';
        return 'eschool_tbl_' + window.location.pathname.replace(/\/+$/, '') + '_' + tableId;
    }

    function getThTitle(th) {
        if (!th) return '';
        const titleEl = th.querySelector('.th-title-text');
        if (titleEl) return titleEl.textContent.trim();
        const clone = th.cloneNode(true);
        clone.querySelectorAll('.th-column-resizer, .th-menu-dropdown, .th-sort-indicator, .th-pin-indicator, input, button, ul, select').forEach(el => el.remove());
        return clone.textContent.trim();
    }

    // --- Column Resizing with Full-Height Guide Line ---
    function initTableResizer(table) {
        if (!table) return;

        const storageKey = getPageKey(table) + '_widths';
        let savedWidths = {};
        try {
            savedWidths = JSON.parse(localStorage.getItem(storageKey) || '{}');
        } catch (e) {}

        const ths = Array.from(table.querySelectorAll('thead th'));
        if (!ths.length) return;

        // Restore saved column widths for new/unrestored columns
        ths.forEach((th, idx) => {
            if (th.dataset.widthRestored === 'true') return;
            th.dataset.widthRestored = 'true';

            const name = getThTitle(th) || ('col_' + idx);
            if (savedWidths[name]) {
                th.style.width = savedWidths[name] + 'px';
                th.style.minWidth = savedWidths[name] + 'px';
            }
        });

        ths.forEach((th, index) => {
            // Don't add resizer to the very last column
            if (index === ths.length - 1) return;

            if (th.dataset.resizerInitialized === 'true') return;
            th.dataset.resizerInitialized = 'true';

            // Ensure relative positioning
            if (window.getComputedStyle(th).position === 'static') {
                th.style.position = 'relative';
            }

            let resizer = th.querySelector('.th-column-resizer');
            if (!resizer) {
                resizer = document.createElement('div');
                resizer.className = 'th-column-resizer';
                resizer.title = 'Kéo để thay đổi độ rộng cột (Nhấp đúp để đặt lại)';
                th.appendChild(resizer);
            }

            let startX = 0;
            let startW = 0;
            let activeTh = null;

            // On Hover: Show subtle vertical guide line down the table
            resizer.addEventListener('mouseenter', function () {
                if (document.body.classList.contains('table-column-resizing-active')) return;
                const rect = th.getBoundingClientRect();
                const tableRect = table.getBoundingClientRect();
                const lineX = rect.right + window.scrollX - 1;
                const lineY = tableRect.top + window.scrollY;
                const lineH = tableRect.height;

                guideLine.style.left = lineX + 'px';
                guideLine.style.top = lineY + 'px';
                guideLine.style.height = lineH + 'px';
                guideLine.style.opacity = '0.4';
                guideLine.style.display = 'block';
            });

            resizer.addEventListener('mouseleave', function () {
                if (!document.body.classList.contains('table-column-resizing-active')) {
                    guideLine.style.display = 'none';
                }
            });

            // On Drag Start: Full-Height Guide Line activates and moves with cursor
            const onMouseDown = function (e) {
                e.preventDefault();
                e.stopPropagation();

                activeTh = th;
                startX = e.pageX;
                startW = activeTh.offsetWidth;

                resizer.classList.add('is-resizing');
                document.body.classList.add('table-column-resizing-active');

                const tableRect = table.getBoundingClientRect();
                const lineY = tableRect.top + window.scrollY;
                const lineH = tableRect.height;

                guideLine.style.top = lineY + 'px';
                guideLine.style.height = lineH + 'px';
                guideLine.style.left = e.pageX + 'px';
                guideLine.style.opacity = '1';
                guideLine.style.display = 'block';

                const onMouseMove = function (ev) {
                    if (!activeTh) return;
                    const diff = ev.pageX - startX;
                    const newW = Math.max(50, startW + diff);

                    activeTh.style.width = newW + 'px';
                    activeTh.style.minWidth = newW + 'px';

                    // Update guide line position down the entire table
                    guideLine.style.left = ev.pageX + 'px';
                };

                const onMouseUp = function () {
                    if (!activeTh) return;
                    resizer.classList.remove('is-resizing');
                    document.body.classList.remove('table-column-resizing-active');
                    guideLine.style.display = 'none';

                    document.removeEventListener('mousemove', onMouseMove);
                    document.removeEventListener('mouseup', onMouseUp);

                    // Save custom widths to localStorage
                    const curWidths = {};
                    ths.forEach((t, i) => {
                        const colName = getThTitle(t) || ('col_' + i);
                        if (t.style.width) {
                            curWidths[colName] = t.offsetWidth;
                        }
                    });
                    try {
                        localStorage.setItem(storageKey, JSON.stringify(curWidths));
                    } catch (err) {}

                    activeTh = null;
                };

                document.addEventListener('mousemove', onMouseMove);
                document.addEventListener('mouseup', onMouseUp);
            };

            resizer.addEventListener('mousedown', onMouseDown);

            // Double click: reset width of this column
            resizer.addEventListener('dblclick', function (e) {
                e.preventDefault();
                e.stopPropagation();
                th.style.width = '';
                th.style.minWidth = '';

                const curWidths = {};
                ths.forEach((t, i) => {
                    const colName = getThTitle(t) || ('col_' + i);
                    if (t.style.width) {
                        curWidths[colName] = t.offsetWidth;
                    }
                });
                try {
                    localStorage.setItem(storageKey, JSON.stringify(curWidths));
                } catch (err) {}
            });
        });

        // Save initial original order of headers for exact restoration
        if (!table._originalThs) {
            table._originalThs = Array.from(table.querySelectorAll('thead th'));
        }

        // --- Column Drag & Drop Reordering (Session Only - Resets on Exit/Reload) ---
        initColumnReordering(table);

        // --- Column Header 3-Dots Menu (Sort & Pin) ---
        initColumnOptionsMenu(table);
    }

    // Move column in all rows of table
    function moveTableColumn(table, fromIndex, toIndex, insertAfter) {
        if (fromIndex === toIndex && !insertAfter) return;
        table.querySelectorAll('tr').forEach(tr => {
            const cells = Array.from(tr.children);
            if (fromIndex < cells.length && toIndex < cells.length) {
                const fromCell = cells[fromIndex];
                const targetCell = cells[toIndex];
                if (insertAfter) {
                    tr.insertBefore(fromCell, targetCell.nextElementSibling);
                } else {
                    tr.insertBefore(fromCell, targetCell);
                }
            }
        });
        updatePinnedColumnStyles(table);
    }

    // Return a TH (and its TD cells) back to its exact initial relative position
    function returnThToOriginalPosition(table, th) {
        if (!table._originalThs) return;
        const origThs = table._originalThs;
        const origIdx = origThs.indexOf(th);
        if (origIdx === -1) return;

        // Find the next sibling in original array that currently exists in the table DOM
        let nextTh = null;
        for (let i = origIdx + 1; i < origThs.length; i++) {
            if (origThs[i].parentNode) {
                nextTh = origThs[i];
                break;
            }
        }

        const currentThs = Array.from(table.querySelectorAll('thead th'));
        const fromIdx = currentThs.indexOf(th);
        if (fromIdx === -1) return;

        if (nextTh) {
            const toIdx = Array.from(table.querySelectorAll('thead th')).indexOf(nextTh);
            if (toIdx !== -1 && toIdx !== fromIdx) {
                table.querySelectorAll('tr').forEach(tr => {
                    const cells = Array.from(tr.children);
                    if (fromIdx < cells.length && toIdx < cells.length) {
                        tr.insertBefore(cells[fromIdx], cells[toIdx]);
                    }
                });
            }
        } else {
            // Was the last column originally
            table.querySelectorAll('tr').forEach(tr => {
                const cells = Array.from(tr.children);
                if (fromIdx < cells.length) {
                    tr.appendChild(cells[fromIdx]);
                }
            });
        }
    }

    // Bold & Highlight all cells of pinned columns
    function updatePinnedColumnStyles(table) {
        const currentThs = Array.from(table.querySelectorAll('thead th'));
        currentThs.forEach((th, colIdx) => {
            const isPinned = th.dataset.isPinned === 'true';
            if (isPinned) {
                th.classList.add('is-pinned-col');
            } else {
                th.classList.remove('is-pinned-col');
            }

            table.querySelectorAll('tbody tr').forEach(tr => {
                const td = tr.children[colIdx];
                if (td) {
                    if (isPinned) {
                        td.classList.add('is-pinned-cell');
                    } else {
                        td.classList.remove('is-pinned-cell');
                    }
                }
            });
        });
    }

    let draggedTh = null;
    let draggedTable = null;

    function initColumnReordering(table) {
        const thead = table.querySelector('thead');
        if (!thead) return;
        const ths = thead.querySelectorAll('th');

        ths.forEach((th, idx) => {
            if (th.dataset.originalIndex === undefined) {
                th.dataset.originalIndex = idx;
            }

            if (th.dataset.reorderInitialized === 'true') return;
            th.dataset.reorderInitialized = 'true';

            // Mark draggable if not purely checkbox column
            if (!th.querySelector('input[type="checkbox"]') || th.innerText.trim()) {
                th.setAttribute('draggable', 'true');
                th.classList.add('is-column-draggable');
            }

            th.addEventListener('dragstart', function (e) {
                // If pinned, or clicking menu/resizer/buttons, don't drag
                if (th.dataset.isPinned === 'true' || e.target.closest('.th-column-resizer, .th-menu-dropdown, input, button, a, select, label')) {
                    e.preventDefault();
                    return;
                }
                draggedTh = th;
                draggedTable = table;
                th.classList.add('th-is-dragging');
                e.dataTransfer.effectAllowed = 'move';
                e.dataTransfer.setData('text/plain', '');
            });

            th.addEventListener('dragover', function (e) {
                if (!draggedTh || draggedTable !== table || draggedTh === th) return;
                // If target column is pinned or pure checkbox column, don't allow reordering
                if (th.dataset.isPinned === 'true') return;

                e.preventDefault();
                e.dataTransfer.dropEffect = 'move';

                const rect = th.getBoundingClientRect();
                const isRightHalf = e.clientX > (rect.left + rect.width / 2);

                th.classList.remove('th-drag-over-left', 'th-drag-over-right');
                if (isRightHalf) {
                    th.classList.add('th-drag-over-right');
                } else {
                    th.classList.add('th-drag-over-left');
                }
            });

            th.addEventListener('dragleave', function () {
                th.classList.remove('th-drag-over-left', 'th-drag-over-right');
            });

            th.addEventListener('drop', function (e) {
                if (!draggedTh || draggedTable !== table || draggedTh === th) return;
                if (th.dataset.isPinned === 'true') return;
                e.preventDefault();

                const currentThs = Array.from(table.querySelectorAll('thead th'));
                const fromIdx = currentThs.indexOf(draggedTh);
                const toIdx = currentThs.indexOf(th);

                const rect = th.getBoundingClientRect();
                const isRightHalf = e.clientX > (rect.left + rect.width / 2);

                if (fromIdx !== -1 && toIdx !== -1) {
                    moveTableColumn(table, fromIdx, toIdx, isRightHalf);
                }

                th.classList.remove('th-drag-over-left', 'th-drag-over-right');
            });

            th.addEventListener('dragend', function () {
                if (draggedTh) {
                    draggedTh.classList.remove('th-is-dragging');
                }
                table.querySelectorAll('thead th').forEach(t => {
                    t.classList.remove('th-drag-over-left', 'th-drag-over-right', 'th-is-dragging');
                });
                draggedTh = null;
                draggedTable = null;
            });
        });
    }

    // Parse cell text for accurate numerical, date, or Vietnamese sorting
    function parseCellValue(text) {
        if (!text) return '';
        const clean = text.trim();
        // Check date dd/MM/yyyy
        const dateMatch = clean.match(/^(\d{1,2})[\/\-](\d{1,2})[\/\-](\d{4})$/);
        if (dateMatch) {
            return new Date(parseInt(dateMatch[3], 10), parseInt(dateMatch[2], 10) - 1, parseInt(dateMatch[1], 10)).getTime();
        }
        // Check pure number or currency (e.g., "1.500.000 đ", "10,5")
        const numStr = clean.replace(/đ|VNĐ|VND|\s/gi, '').replace(/\./g, '').replace(',', '.');
        if (!isNaN(numStr) && numStr !== '') {
            return parseFloat(numStr);
        }
        return clean;
    }

    function sortTableByColumn(table, th, order = 'asc') {
        const tbody = table.querySelector('tbody');
        if (!tbody) return;

        // Ensure all rows have original row index stored
        Array.from(tbody.querySelectorAll('tr')).forEach((tr, rIdx) => {
            if (tr.dataset.originalRowIndex === undefined) {
                tr.dataset.originalRowIndex = rIdx;
            }
        });

        const currentThs = Array.from(table.querySelectorAll('thead th'));
        const colIdx = currentThs.indexOf(th);
        if (colIdx === -1) return;

        const rows = Array.from(tbody.querySelectorAll('tr'));
        if (rows.length <= 1) return;

        if (order === 'none') {
            // Restore original row order
            rows.sort((a, b) => (parseInt(a.dataset.originalRowIndex, 10) || 0) - (parseInt(b.dataset.originalRowIndex, 10) || 0));
        } else {
            rows.sort((rowA, rowB) => {
                const cellA = rowA.children[colIdx];
                const cellB = rowB.children[colIdx];
                const valA = cellA ? parseCellValue(cellA.innerText) : '';
                const valB = cellB ? parseCellValue(cellB.innerText) : '';

                if (typeof valA === 'number' && typeof valB === 'number') {
                    return order === 'asc' ? valA - valB : valB - valA;
                }

                const strA = String(valA);
                const strB = String(valB);
                const comp = strA.localeCompare(strB, 'vi', { numeric: true, sensitivity: 'base' });
                return order === 'asc' ? comp : -comp;
            });
        }

        rows.forEach(r => tbody.appendChild(r));

        // Update sort indicators on all headers
        currentThs.forEach(h => {
            const ind = h.querySelector('.th-sort-indicator');
            if (!ind) return;
            if (h === th) {
                h.dataset.sortOrder = order;
                if (order === 'asc') {
                    ind.innerHTML = '<i class="bi bi-chevron-up text-primary fw-bold"></i>';
                } else if (order === 'desc') {
                    ind.innerHTML = '<i class="bi bi-chevron-down text-primary fw-bold"></i>';
                } else {
                    ind.innerHTML = '<i class="bi bi-chevron-expand"></i>';
                }
            } else {
                h.dataset.sortOrder = 'none';
                ind.innerHTML = '<i class="bi bi-chevron-expand"></i>';
            }
        });
    }

    // --- 3-Dots Menu on Column Header ---
    function initColumnOptionsMenu(table) {
        const thead = table.querySelector('thead');
        if (!thead) return;
        const ths = thead.querySelectorAll('th');

        // Store original row indices on tbody
        const tbody = table.querySelector('tbody');
        if (tbody) {
            Array.from(tbody.querySelectorAll('tr')).forEach((tr, rIdx) => {
                if (tr.dataset.originalRowIndex === undefined) {
                    tr.dataset.originalRowIndex = rIdx;
                }
            });
        }

        ths.forEach(th => {
            if (th.dataset.menuInitialized === 'true') return;

            // Skip pure checkbox header column with no text
            const hasCheckboxOnly = th.classList.contains('app-table-checkbox-col') || (th.querySelector('input[type="checkbox"]') && !getThTitle(th));
            if (hasCheckboxOnly) return;

            th.dataset.menuInitialized = 'true';

            // Wrap existing content if not already wrapped
            let wrapper = th.querySelector('.th-content-wrapper');
            if (!wrapper) {
                wrapper = document.createElement('div');
                wrapper.className = 'th-content-wrapper';

                const pinIcon = document.createElement('span');
                pinIcon.className = 'th-pin-indicator';
                pinIcon.innerHTML = '<i class="bi bi-pin-angle-fill"></i>';
                pinIcon.style.display = 'none';

                const titleSpan = document.createElement('span');
                titleSpan.className = 'th-title-text';
                
                // Move text / child nodes except resizer into titleSpan
                const resizer = th.querySelector('.th-column-resizer');
                while (th.firstChild) {
                    if (th.firstChild === resizer) break;
                    titleSpan.appendChild(th.firstChild);
                }

                const sortIcon = document.createElement('span');
                sortIcon.className = 'th-sort-indicator';
                sortIcon.innerHTML = '<i class="bi bi-chevron-expand"></i>';

                const menuDropdown = document.createElement('div');
                menuDropdown.className = 'dropdown th-menu-dropdown';
                menuDropdown.innerHTML = `
                    <button type="button" class="btn th-menu-btn" data-bs-toggle="dropdown" aria-expanded="false" title="Tùy chọn cột">
                        <i class="bi bi-three-dots-vertical"></i>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end th-dropdown-panel shadow-sm">
                        <li>
                            <button type="button" class="dropdown-item btn-col-sort-asc">
                                <i class="bi bi-arrow-up me-2"></i><span>Sắp xếp tăng dần</span>
                            </button>
                        </li>
                        <li>
                            <button type="button" class="dropdown-item btn-col-sort-desc">
                                <i class="bi bi-arrow-down me-2"></i><span>Sắp xếp giảm dần</span>
                            </button>
                        </li>
                        <li><hr class="dropdown-divider my-1"></li>
                        <li>
                            <button type="button" class="dropdown-item btn-col-toggle-pin">
                                <i class="bi bi-pin-angle me-2"></i><span class="pin-label">Cố định cột</span>
                            </button>
                        </li>
                    </ul>
                `;

                wrapper.appendChild(pinIcon);
                wrapper.appendChild(titleSpan);
                wrapper.appendChild(sortIcon);
                wrapper.appendChild(menuDropdown);

                if (resizer) {
                    th.insertBefore(wrapper, resizer);
                } else {
                    th.appendChild(wrapper);
                }
            }

            // Quick click on sort indicator: None -> Asc (1st) -> Desc (2nd) -> None (3rd, reset icon)
            const sortInd = wrapper.querySelector('.th-sort-indicator');
            if (sortInd) {
                sortInd.addEventListener('click', function (e) {
                    e.stopPropagation();
                    const cur = th.dataset.sortOrder || 'none';
                    let nextOrder = 'asc';
                    if (cur === 'asc') {
                        nextOrder = 'desc';
                    } else if (cur === 'desc') {
                        nextOrder = 'none';
                    } else {
                        nextOrder = 'asc';
                    }
                    sortTableByColumn(table, th, nextOrder);
                });
            }

            // Menu actions
            const btnSortAsc = wrapper.querySelector('.btn-col-sort-asc');
            if (btnSortAsc) {
                btnSortAsc.addEventListener('click', function (e) {
                    e.stopPropagation();
                    th.dataset.sortOrder = 'asc';
                    sortTableByColumn(table, th, 'asc');
                });
            }

            const btnSortDesc = wrapper.querySelector('.btn-col-sort-desc');
            if (btnSortDesc) {
                btnSortDesc.addEventListener('click', function (e) {
                    e.stopPropagation();
                    th.dataset.sortOrder = 'desc';
                    sortTableByColumn(table, th, 'desc');
                });
            }

            const btnTogglePin = wrapper.querySelector('.btn-col-toggle-pin');
            if (btnTogglePin) {
                btnTogglePin.addEventListener('click', function (e) {
                    e.stopPropagation();
                    const isCurrentlyPinned = th.dataset.isPinned === 'true';
                    const pinIndicator = wrapper.querySelector('.th-pin-indicator');
                    const pinIconEl = btnTogglePin.querySelector('i');
                    const pinLabelEl = btnTogglePin.querySelector('.pin-label');

                    if (isCurrentlyPinned) {
                        // Unpin
                        th.dataset.isPinned = 'false';
                        th.classList.remove('is-pinned', 'is-pinned-col');
                        th.setAttribute('draggable', 'true');
                        if (pinIndicator) pinIndicator.style.display = 'none';

                        if (pinIconEl) pinIconEl.className = 'bi bi-pin-angle me-2';
                        if (pinLabelEl) pinLabelEl.textContent = 'Cố định cột';

                        // Restore exact original position
                        returnThToOriginalPosition(table, th);
                        updatePinnedColumnStyles(table);
                    } else {
                        // Pin: Push column to the very start of table
                        th.dataset.isPinned = 'true';
                        th.classList.add('is-pinned', 'is-pinned-col');
                        th.setAttribute('draggable', 'false');
                        if (pinIndicator) pinIndicator.style.display = 'inline-flex';

                        if (pinIconEl) pinIconEl.className = 'bi bi-pin-angle-fill me-2 text-primary';
                        if (pinLabelEl) pinLabelEl.textContent = 'Bỏ cố định cột';

                        // Find first position (after checkbox column if exists)
                        const currentThs = Array.from(table.querySelectorAll('thead th'));
                        const curIdx = currentThs.indexOf(th);
                        const hasCheckboxAt0 = currentThs[0] && (currentThs[0].classList.contains('app-table-checkbox-col') || (currentThs[0].querySelector('input[type="checkbox"]') && !getThTitle(currentThs[0])));
                        const targetIdx = hasCheckboxAt0 ? 1 : 0;

                        if (curIdx !== -1 && curIdx !== targetIdx) {
                            table.querySelectorAll('tr').forEach(tr => {
                                const cells = Array.from(tr.children);
                                if (curIdx < cells.length && targetIdx < cells.length) {
                                    tr.insertBefore(cells[curIdx], cells[targetIdx]);
                                }
                            });
                        }
                        updatePinnedColumnStyles(table);
                    }
                });
            }
        });
    }

    // --- Column Visibility Manager ---
    function initColumnManager(managerEl) {
        if (!managerEl || managerEl.dataset.managerInitialized === 'true') return;
        managerEl.dataset.managerInitialized = 'true';

        const card = managerEl.closest('.content-card, .page-card, .card, .student-admin-card, .teacher-admin-card') || document;
        const table = card.querySelector('table');
        if (!table) return;

        const storageKey = getPageKey(table) + '_hidden';
        const listContainer = managerEl.querySelector('.column-checkbox-list');
        const toggleAllBtn = managerEl.querySelector('.toggle-all-columns-btn');
        const searchInput = managerEl.querySelector('.column-search-input');
        const resetBtn = managerEl.querySelector('.reset-columns-btn');

        if (!listContainer) return;

        const ths = Array.from(table.querySelectorAll('thead th'));
        const columns = [];

        ths.forEach((th, index) => {
            let name = getThTitle(th);
            if (!name && th.querySelector('input[type="checkbox"]')) return;
            if (!name) name = 'Cột ' + (index + 1);

            columns.push({
                index: index,
                name: name,
                th: th
            });
        });

        function getHiddenCols() {
            try {
                return JSON.parse(localStorage.getItem(storageKey) || '[]');
            } catch (e) {
                return [];
            }
        }

        function saveHiddenCols(arr) {
            try {
                localStorage.setItem(storageKey, JSON.stringify(arr));
            } catch (e) {}
        }

        function setColVisibility(colOrName, isVisible) {
            const currentThs = Array.from(table.querySelectorAll('thead th'));
            let targetTh = null;
            let targetIdx = -1;

            if (colOrName && typeof colOrName === 'object' && colOrName.th) {
                targetTh = colOrName.th;
                targetIdx = currentThs.indexOf(targetTh);
            } else if (typeof colOrName === 'string') {
                targetIdx = currentThs.findIndex(t => getThTitle(t) === colOrName);
                if (targetIdx !== -1) targetTh = currentThs[targetIdx];
            } else if (typeof colOrName === 'number') {
                targetIdx = colOrName;
                targetTh = currentThs[targetIdx];
            }

            if (targetTh) targetTh.style.display = isVisible ? '' : 'none';
            if (targetIdx !== -1) {
                table.querySelectorAll('tbody tr').forEach(tr => {
                    const td = tr.children[targetIdx];
                    if (td) td.style.display = isVisible ? '' : 'none';
                });
            }
        }

        // Apply saved visibility
        const hiddenCols = getHiddenCols();
        columns.forEach(col => {
            const isHidden = hiddenCols.includes(col.name);
            setColVisibility(col, !isHidden);
        });

        function renderList(term = '') {
            listContainer.innerHTML = '';
            term = term.trim().toLowerCase();

            let visibleCount = 0;
            let total = 0;
            const curHidden = getHiddenCols();

            columns.forEach(col => {
                if (term && !col.name.toLowerCase().includes(term)) return;
                total++;
                const isChecked = !curHidden.includes(col.name);
                if (isChecked) visibleCount++;

                const item = document.createElement('label');
                item.className = 'column-manager-item';
                item.innerHTML = `
                    <input type="checkbox" class="form-check-input mt-0 column-item-checkbox" data-col-name="${col.name}" ${isChecked ? 'checked' : ''} />
                    <span class="small text-dark">${col.name}</span>
                `;

                item.querySelector('.column-item-checkbox').addEventListener('change', function () {
                    const cName = this.getAttribute('data-col-name');
                    const isVis = this.checked;

                    setColVisibility(cName, isVis);

                    const nowHidden = getHiddenCols();
                    if (isVis) {
                        const i = nowHidden.indexOf(cName);
                        if (i > -1) nowHidden.splice(i, 1);
                    } else {
                        if (!nowHidden.includes(cName)) nowHidden.push(cName);
                    }
                    saveHiddenCols(nowHidden);
                    updateToggleText();
                });

                listContainer.appendChild(item);
            });

            if (total === 0) {
                listContainer.innerHTML = '<div class="text-muted text-center py-2 small">Không tìm thấy cột</div>';
            }

            updateToggleText();
        }

        function updateToggleText() {
            if (!toggleAllBtn) return;
            const nowHidden = getHiddenCols();
            const allHidden = columns.every(c => nowHidden.includes(c.name));
            toggleAllBtn.textContent = allHidden ? 'Hiện tất cả' : 'Ẩn tất cả';
        }

        if (searchInput) {
            searchInput.addEventListener('input', function () {
                renderList(this.value);
            });
        }

        if (toggleAllBtn) {
            toggleAllBtn.addEventListener('click', function () {
                const nowHidden = getHiddenCols();
                const allHidden = columns.every(c => nowHidden.includes(c.name));

                if (allHidden) {
                    saveHiddenCols([]);
                    columns.forEach(c => setColVisibility(c.index, true));
                } else {
                    saveHiddenCols(columns.map(c => c.name));
                    columns.forEach(c => setColVisibility(c.index, false));
                }
                renderList(searchInput ? searchInput.value : '');
            });
        }

        if (resetBtn) {
            resetBtn.addEventListener('click', function () {
                localStorage.removeItem(storageKey);
                localStorage.removeItem(getPageKey(table) + '_widths');
                columns.forEach(c => {
                    setColVisibility(c.index, true);
                    if (c.th) {
                        c.th.style.width = '';
                        c.th.style.minWidth = '';
                    }
                });
                if (searchInput) searchInput.value = '';
                renderList('');
            });
        }

        renderList('');
    }

    let isInternalDOMChange = false;

    function initAll() {
        if (isInternalDOMChange) return;
        isInternalDOMChange = true;
        try {
            document.querySelectorAll('.column-manager-dropdown').forEach(initColumnManager);
            document.querySelectorAll('table').forEach(table => {
                initTableResizer(table);
                initColumnOptionsMenu(table);
                initColumnReordering(table);
            });
        } finally {
            setTimeout(() => { isInternalDOMChange = false; }, 60);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    // Dynamic handling on BS dropdown
    document.addEventListener('shown.bs.dropdown', function (e) {
        const dropdown = e.target.closest('.column-manager-dropdown');
        if (dropdown && dropdown.dataset.managerInitialized !== 'true') initColumnManager(dropdown);
    });

    // Observe any new tables or newly inserted headers with debouncing & change filtering
    let domChangeTimer = null;
    const observer = new MutationObserver(function (mutations) {
        if (isInternalDOMChange) return;

        let hasRelevantChange = false;
        for (const m of mutations) {
            for (const node of m.addedNodes) {
                if (node.nodeType === 1) {
                    if (node.tagName === 'TABLE' || node.tagName === 'TH') {
                        if (!node.classList.contains('th-content-wrapper') && !node.classList.contains('th-column-resizer')) {
                            hasRelevantChange = true;
                            break;
                        }
                    } else if (node.querySelector && node.querySelector('table, th:not([data-menu-initialized="true"])')) {
                        hasRelevantChange = true;
                        break;
                    }
                }
            }
            if (hasRelevantChange) break;
        }

        if (hasRelevantChange) {
            clearTimeout(domChangeTimer);
            domChangeTimer = setTimeout(initAll, 80);
        }
    });

    observer.observe(document.body, { childList: true, subtree: true });
})();
