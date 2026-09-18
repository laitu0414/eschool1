document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("input, select, textarea").forEach(control => {
        const isRequired = control.required || control.hasAttribute("data-val-required");
        if (!isRequired || control.type === "hidden")
            return;

        const container = control.closest(".mb-3, .col-4, .col-6, .col-8, [class*='col-md-'], [class*='col-lg-']");
        const label = container?.querySelector("label");
        if (!label || label.textContent.includes("*"))
            return;

        const mark = document.createElement("span");
        mark.className = "text-danger required-mark";
        mark.textContent = " *";
        label.appendChild(mark);
    });

    // Auto-generate STT column for all data tables
    document.querySelectorAll("table").forEach(table => {
        // Skip specialized tables like calendar, timetable, etc.
        if (table.classList.contains("no-stt")) return;
        if (table.dataset.sttGenerated === "true") return;

        const thead = table.querySelector("thead");
        const tbody = table.querySelector("tbody");
        if (!thead || !tbody) return;

        const allThs = Array.from(thead.querySelectorAll("th, td"));
        const alreadyHasStt = allThs.some(th => {
            const titleEl = th.querySelector(".th-title-text");
            let text = "";
            if (titleEl) {
                text = titleEl.textContent.trim().toLowerCase();
            } else {
                const clone = th.cloneNode(true);
                clone.querySelectorAll(".th-column-resizer, .th-menu-dropdown, .th-sort-indicator, .th-pin-indicator, input, button, ul, select").forEach(el => el.remove());
                text = clone.textContent.trim().toLowerCase();
            }
            return text === "stt" || text === "số tt" || text === "#" || text === "no." || 
                   text === "thứ" || text === "tiết" || text === "thời gian" || text.includes("giờ");
        });

        if (alreadyHasStt) {
            table.dataset.sttGenerated = "true";
            return;
        }

        table.dataset.sttGenerated = "true";

        thead.querySelectorAll("tr").forEach(tr => {
            const th = document.createElement("th");
            th.textContent = "STT";
            th.style.width = "60px";
            th.className = "text-center";
            const firstChild = tr.firstElementChild;
            if (firstChild && firstChild.classList.contains("app-table-checkbox-col")) {
                tr.insertBefore(th, firstChild.nextElementSibling);
            } else {
                tr.insertBefore(th, tr.firstElementChild);
            }
        });

        let stt = 1;
        tbody.querySelectorAll("tr").forEach(tr => {
            const tds = tr.querySelectorAll("td, th");
            if (tds.length === 1 && tds[0].hasAttribute("colspan")) {
                const cs = parseInt(tds[0].getAttribute("colspan"), 10);
                tds[0].setAttribute("colspan", cs + 1);
            } else {
                const td = document.createElement("td");
                td.textContent = stt++;
                td.className = "text-center text-muted fw-medium align-middle";
                const firstChild = tr.firstElementChild;
                if (firstChild && firstChild.classList.contains("app-table-checkbox-cell")) {
                    tr.insertBefore(td, firstChild.nextElementSibling);
                } else {
                    tr.insertBefore(td, tr.firstElementChild);
                }
            }
        });
    });
});
