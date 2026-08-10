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
        const thead = table.querySelector("thead");
        const tbody = table.querySelector("tbody");
        if (!thead || !tbody) return;

        const firstHeader = thead.querySelector("th, td");
        if (!firstHeader) return;

        const headerText = firstHeader.textContent.trim().toLowerCase();
        if (headerText === "stt" || headerText === "số tt" || headerText === "thứ" || 
            headerText === "tiết" || headerText === "thời gian" || headerText.includes("giờ")) return;

        thead.querySelectorAll("tr").forEach(tr => {
            const th = document.createElement("th");
            th.textContent = "STT";
            th.style.width = "60px";
            th.className = "text-center";
            tr.insertBefore(th, tr.firstChild);
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
                tr.insertBefore(td, tr.firstChild);
            }
        });
    });
});
