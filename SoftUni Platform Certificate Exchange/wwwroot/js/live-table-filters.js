document.addEventListener("DOMContentLoaded", () => {

    // Откриваме формуляра за филтриране на текущата страница
    const form = document.querySelector("form[data-live-filters]");

    if (!form) return;

    // Намираме таблицата и броя резултати
    const tableBody = form.querySelector("table tbody");
    const resultCount = form.querySelector("[data-live-count]");

    if (!tableBody || !resultCount) return;

    let debounceTimer;
    let activeController;
    let requestVersion = 0;

    // Планира обновяване след зададено изчакване
    function scheduleRefresh(delay = 0) {

        clearTimeout(debounceTimer);

        // Прекратяваме предходната HTTP заявка
        if (activeController) {
            activeController.abort();
        }

        // Номер на актуалната заявка
        const version = ++requestVersion;

        debounceTimer = setTimeout(() => {
            refreshResults(version);
        }, delay);
    }

    // Зарежда актуалните резултати от MVC Controller
    async function refreshResults(version) {

        // Събираме всички стойности от формуляра
        const url = new URL(form.action, window.location.href);

        url.search = new URLSearchParams(
            new FormData(form)
        ).toString();

        // Позволява прекратяване на стара заявка
        const controller = new AbortController();
        activeController = controller;

        try {
            // Изпращаме GET заявка без пълно презареждане
            const response = await fetch(url.toString(), {
                method: "GET",
                credentials: "same-origin",
                signal: controller.signal
            });

            // При пренасочване към Login или Change Password
            if (response.redirected) {
                window.location.assign(response.url);
                return;
            }

            if (!response.ok) {
                throw new Error(`HTTP ${response.status}`);
            }

            // Получаваме HTML от съществуващия MVC Action
            const html = await response.text();

            // Игнорираме остарял резултат
            if (version !== requestVersion) return;

            // Прочитаме HTML, без да го показваме като нова страница
            const documentResult = new DOMParser()
                .parseFromString(html, "text/html");

            const updatedForm = documentResult
                .querySelector("form[data-live-filters]");

            const updatedBody = updatedForm
                ?.querySelector("table tbody");

            const updatedCount = updatedForm
                ?.querySelector("[data-live-count]");

            if (!updatedBody || !updatedCount) {
                throw new Error("Filter results not found.");
            }

            // Подменяме само редовете в таблицата
            tableBody.innerHTML = updatedBody.innerHTML;

            // Обновяваме броя намерени записи
            resultCount.textContent = updatedCount.textContent;

            // Актуализираме URL, без презареждане
            window.history.replaceState(
                window.history.state,
                "",
                url.toString()
            );

        } catch (error) {

            // Прекратените заявки не са грешки
            if (error.name === "AbortError" ||
                version !== requestVersion) {
                return;
            }

            console.error("Live filtering failed:", error);

            // Резервен вариант: обикновено GET зареждане
            window.location.assign(url.toString());

        } finally {
            if (activeController === controller) {
                activeController = null;
            }
        }
    }

    // LIVE SEARCH: автоматично след 400 ms пауза
    form.addEventListener("input", (event) => {

        if (event.target.matches('input[type="search"]')) {
            scheduleRefresh(400);
        }

    });

    // LIVE FILTER: дати, Status и Group By
    form.addEventListener("change", (event) => {

        if (event.target.matches('input[type="date"], select')) {
            scheduleRefresh(0);
        }

    });

    // SORTING и ръчните Search бутони
    form.addEventListener("submit", (event) => {

        // Спираме презареждането на цялата страница
        event.preventDefault();

        // Изпращаме всички актуални филтри
        scheduleRefresh(0);
    });

});
