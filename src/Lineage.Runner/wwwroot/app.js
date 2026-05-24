const scenarioSelect = document.querySelector("#scenarioPath");
const runsBody = document.querySelector("#runsBody");
const launchForm = document.querySelector("#launchForm");
const formMessage = document.querySelector("#formMessage");
const refreshStatus = document.querySelector("#refreshStatus");
const refreshButton = document.querySelector("#refreshButton");
const runSearch = document.querySelector("#runSearch");
const statusFilter = document.querySelector("#statusFilter");
const scenarioFilter = document.querySelector("#scenarioFilter");
const selectAllRuns = document.querySelector("#selectAllRuns");
const bulkDeleteButton = document.querySelector("#bulkDeleteButton");
const selectionStatus = document.querySelector("#selectionStatus");

let refreshTimer = null;
let allRuns = [];
let selectedRunIds = new Set();
let expandedRunId = null;
let runDetailsById = new Map();
let sortKey = "createdAtUtc";
let sortDirection = "desc";

async function loadScenarios() {
  const response = await fetch("/api/scenarios");
  const scenarios = await response.json();
  scenarioSelect.innerHTML = "";
  for (const scenario of scenarios) {
    const option = document.createElement("option");
    option.value = scenario.path;
    option.textContent = `${scenario.name} (${scenario.path})`;
    scenarioSelect.append(option);
  }
}

async function loadRuns() {
  refreshStatus.textContent = "Refreshing";
  const response = await fetch("/api/runs");
  allRuns = await response.json();
  const liveIds = new Set(allRuns.map((run) => run.id));
  selectedRunIds = new Set([...selectedRunIds].filter((id) => liveIds.has(id)));
  if (expandedRunId && !liveIds.has(expandedRunId)) {
    expandedRunId = null;
  }

  populateRunScenarioFilter();
  renderRuns();
  refreshStatus.textContent = `Updated ${new Date().toLocaleTimeString()}`;
}

function populateRunScenarioFilter() {
  const selected = scenarioFilter.value;
  const scenarios = [...new Map(allRuns.map((run) => [
    run.scenarioPath,
    run.scenarioName || run.scenarioPath
  ])).entries()].sort((left, right) => left[1].localeCompare(right[1]));

  scenarioFilter.innerHTML = `<option value="all">All scenarios</option>`;
  for (const [path, name] of scenarios) {
    const option = document.createElement("option");
    option.value = path;
    option.textContent = name;
    scenarioFilter.append(option);
  }

  scenarioFilter.value = scenarios.some(([path]) => path === selected) ? selected : "all";
}

function renderRuns() {
  const runs = getVisibleRuns();
  runsBody.innerHTML = "";
  updateSortIndicators();
  if (runs.length === 0) {
    const row = document.createElement("tr");
    row.innerHTML = `<td class="empty" colspan="8">No runs match the current filters.</td>`;
    runsBody.append(row);
    updateSelectionControls(runs);
    return;
  }

  for (const run of runs) {
    const row = document.createElement("tr");
    const progress = Math.max(0, Math.min(1, run.progress || 0));
    const canSelect = !run.isRunning;
    const isExpanded = expandedRunId === run.id;
    row.className = isExpanded ? "is-expanded" : "";
    row.innerHTML = `
      <td>
        <input class="run-select" type="checkbox" data-id="${escapeHtml(run.id)}" ${selectedRunIds.has(run.id) ? "checked" : ""} ${canSelect ? "" : "disabled"} aria-label="Select run ${escapeHtml(run.name)}">
      </td>
      <td>
        <div class="run-name">${escapeHtml(run.name)}</div>
        <div class="run-sub">${escapeHtml(run.scenarioPath)}</div>
        <div class="run-sub">seed ${escapeHtml(formatSeed(run.seed))}</div>
        <div class="run-sub">${escapeHtml(run.id)}</div>
      </td>
      <td>
        <span class="run-state ${statusClass(run.status)}">${escapeHtml(run.status || "unknown")}</span>
        ${run.stopReason ? `<div class="run-sub">${escapeHtml(run.stopReason)}</div>` : ""}
      </td>
      <td>
        <div>${Math.round(progress * 1000) / 10}%</div>
        <div class="progress-track"><div class="progress-fill" style="width:${progress * 100}%"></div></div>
        <div class="run-sub">step ${formatNumber(run.completedSteps)} / ${formatNumber(run.ticks)}</div>
        <div class="run-sub">tick ${formatNumber(run.currentTick)}</div>
      </td>
      <td>
        <div class="metric-grid">
          <span>Creatures</span><strong>${formatNumber(run.creatureCount)}</strong>
          <span>Eggs</span><strong>${formatNumber(run.eggCount)}</strong>
          <span>Species</span><strong>${formatNumber(run.speciesClusterCount)}</strong>
          <span>Gen</span><strong>${formatNumber(run.maxGeneration)}</strong>
        </div>
      </td>
      <td>
        <div class="metric-grid">
          <span>Seed</span><strong>${escapeHtml(formatSeed(run.seed))}</strong>
          <span>Births</span><strong>${formatNumber(run.creatureBirthCount)}</strong>
          <span>Deaths</span><strong>${formatNumber(run.creatureDeathCount)}</strong>
          <span>Checkpoints</span><strong>${formatNumber(run.checkpointCount)}</strong>
          <span>PID</span><strong>${run.processId ?? ""}</strong>
          <span>Exit</span><strong>${run.exitCode ?? ""}</strong>
        </div>
      </td>
      <td>
        <div class="run-sub">${escapeHtml(run.runDirectory)}</div>
        ${run.latestCheckpointPath ? `<div class="run-sub">Latest: ${escapeHtml(run.latestCheckpointPath)}</div>` : ""}
      </td>
      <td>
        <div class="actions">
          <button class="secondary" data-action="details" data-id="${escapeHtml(run.id)}">${isExpanded ? "Hide" : "Details"}</button>
          <button class="secondary" data-action="rename" data-id="${escapeHtml(run.id)}">Rename</button>
          <button class="secondary" data-action="checkpoint" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "" : "disabled"}>Checkpoint</button>
          <button class="secondary" data-action="checkpoint-stop" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "" : "disabled"}>Checkpoint + Stop</button>
          <button class="secondary" data-action="stop" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "" : "disabled"}>Stop</button>
          <button class="secondary" data-action="report" data-id="${escapeHtml(run.id)}" ${run.hasReport ? "" : "disabled"}>Report</button>
          <button class="danger" data-action="delete" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "disabled" : ""}>Delete</button>
        </div>
      </td>
    `;
    runsBody.append(row);

    if (isExpanded) {
      runsBody.append(renderDetailsRow(run));
    }
  }

  updateSelectionControls(runs);
}

function renderDetailsRow(run) {
  const row = document.createElement("tr");
  row.className = "details-row";
  const details = runDetailsById.get(run.id);
  if (!details || details.loading) {
    row.innerHTML = `<td colspan="8"><div class="details-panel">Loading...</div></td>`;
    return row;
  }

  if (details.loadError) {
    row.innerHTML = `<td colspan="8"><div class="details-panel">${escapeHtml(details.loadError)}</div></td>`;
    return row;
  }

  row.innerHTML = `
    <td colspan="8">
      <div class="details-panel">
        <div class="details-grid">
          <div><span>Created</span><strong>${escapeHtml(formatDateTime(details.run.createdAtUtc))}</strong></div>
          <div><span>Started</span><strong>${escapeHtml(formatDateTime(details.run.startedAtUtc))}</strong></div>
          <div><span>Ended</span><strong>${escapeHtml(formatDateTime(details.run.endedAtUtc))}</strong></div>
          <div><span>Seed</span><strong>${escapeHtml(formatSeed(details.run.seed))}</strong></div>
          <div><span>Stats</span><strong>${escapeHtml(details.run.statsPath)}</strong></div>
          <div><span>Snapshot</span><strong>${escapeHtml(details.run.snapshotPath)}</strong></div>
        </div>
        ${details.error ? `<div class="detail-error">${escapeHtml(details.error)}</div>` : ""}
        <div class="command-line">${escapeHtml(details.commandLine || "")}</div>
        <div class="log-grid">
          <div>
            <div class="log-title">stdout.log</div>
            <pre>${escapeHtml(formatLog(details.stdoutTail))}</pre>
          </div>
          <div>
            <div class="log-title">stderr.log</div>
            <pre>${escapeHtml(formatLog(details.stderrTail))}</pre>
          </div>
        </div>
      </div>
    </td>
  `;
  return row;
}

function getVisibleRuns() {
  const query = runSearch.value.trim().toLowerCase();
  const status = statusFilter.value;
  const scenario = scenarioFilter.value;

  return sortRuns(allRuns.filter((run) => {
    if (scenario !== "all" && run.scenarioPath !== scenario) {
      return false;
    }

    if (!matchesStatusFilter(run, status)) {
      return false;
    }

    if (query.length === 0) {
      return true;
    }

    return [
      run.name,
      run.id,
      run.scenarioName,
      run.scenarioPath,
      run.status,
      run.seed,
      run.processId
    ].some((value) => String(value ?? "").toLowerCase().includes(query));
  }));
}

function sortRuns(runs) {
  return [...runs].sort((left, right) => {
    const leftValue = sortValue(left, sortKey);
    const rightValue = sortValue(right, sortKey);
    const result = typeof leftValue === "number" && typeof rightValue === "number"
      ? leftValue - rightValue
      : String(leftValue).localeCompare(String(rightValue), undefined, { numeric: true, sensitivity: "base" });

    if (result === 0) {
      return Date.parse(right.createdAtUtc || 0) - Date.parse(left.createdAtUtc || 0);
    }

    return sortDirection === "asc" ? result : -result;
  });
}

function sortValue(run, key) {
  switch (key) {
    case "name":
      return run.name || "";
    case "status":
      return run.status || "";
    case "progress":
      return Number(run.progress || 0);
    case "creatures":
      return Number(run.creatureCount || 0);
    case "seed":
      return Number(run.seed ?? -1);
    case "createdAtUtc":
    default:
      return Date.parse(run.createdAtUtc || 0);
  }
}

function matchesStatusFilter(run, filter) {
  const status = String(run.status || "").toLowerCase();
  if (filter === "active") {
    return run.isRunning || ["running", "starting", "stopping"].includes(status);
  }

  if (filter === "completed") {
    return status === "completed";
  }

  if (filter === "problem") {
    return ["failed", "lost", "unknown"].includes(status);
  }

  return true;
}

function updateSelectionControls(runs = getVisibleRuns()) {
  const visibleSelectableIds = runs.filter((run) => !run.isRunning).map((run) => run.id);
  const selectedVisibleCount = visibleSelectableIds.filter((id) => selectedRunIds.has(id)).length;
  selectAllRuns.checked = visibleSelectableIds.length > 0 && selectedVisibleCount === visibleSelectableIds.length;
  selectAllRuns.indeterminate = selectedVisibleCount > 0 && selectedVisibleCount < visibleSelectableIds.length;
  selectAllRuns.disabled = visibleSelectableIds.length === 0;
  bulkDeleteButton.disabled = selectedRunIds.size === 0;
  selectionStatus.textContent = `${selectedRunIds.size} selected`;
}

function updateSortIndicators() {
  for (const button of document.querySelectorAll("[data-sort]")) {
    button.classList.toggle("is-active", button.dataset.sort === sortKey);
  }

  for (const indicator of document.querySelectorAll("[data-sort-indicator]")) {
    indicator.textContent = indicator.dataset.sortIndicator === sortKey ? sortDirection : "";
  }
}

launchForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  formMessage.textContent = "Starting run...";
  const checkpointInterval = valueOrNull("#checkpointInterval");
  const payload = {
    scenarioPath: scenarioSelect.value,
    ticks: Number(document.querySelector("#ticks").value),
    seed: valueOrNull("#seed"),
    checkpointIntervalTicks: checkpointInterval && checkpointInterval > 0 ? checkpointInterval : null,
    stopOnExtinction: document.querySelector("#stopOnExtinction").checked
  };

  const response = await fetch("/api/runs", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Run launch failed." }));
    formMessage.textContent = problem.error || "Run launch failed.";
    return;
  }

  const run = await response.json();
  formMessage.textContent = `Started ${run.name}.`;
  await loadRuns();
});

runsBody.addEventListener("change", (event) => {
  const checkbox = event.target.closest(".run-select");
  if (!checkbox) {
    return;
  }

  if (checkbox.checked) {
    selectedRunIds.add(checkbox.dataset.id);
  } else {
    selectedRunIds.delete(checkbox.dataset.id);
  }

  updateSelectionControls();
});

runsBody.addEventListener("click", async (event) => {
  const button = event.target.closest("button[data-action]");
  if (!button) {
    return;
  }

  const action = button.dataset.action;
  const id = button.dataset.id;
  if (action === "report") {
    window.open(`/api/runs/${encodeURIComponent(id)}/report`, "_blank", "noopener");
    return;
  }

  if (action === "rename") {
    await renameRun(id);
    return;
  }

  if (action === "details") {
    await toggleRunDetails(id);
    return;
  }

  if (action === "delete" && !confirm("Delete this run and its artifacts?")) {
    return;
  }

  const url = action === "delete"
    ? `/api/runs/${encodeURIComponent(id)}`
    : `/api/runs/${encodeURIComponent(id)}/${action}`;
  await fetch(url, { method: action === "delete" ? "DELETE" : "POST" });
  selectedRunIds.delete(id);
  await loadRuns();
});

selectAllRuns.addEventListener("change", () => {
  const visibleSelectableIds = getVisibleRuns().filter((run) => !run.isRunning).map((run) => run.id);
  if (selectAllRuns.checked) {
    for (const id of visibleSelectableIds) {
      selectedRunIds.add(id);
    }
  } else {
    for (const id of visibleSelectableIds) {
      selectedRunIds.delete(id);
    }
  }

  renderRuns();
});

bulkDeleteButton.addEventListener("click", async () => {
  const ids = [...selectedRunIds];
  if (ids.length === 0 || !confirm(`Delete ${ids.length} selected run(s) and their artifacts?`)) {
    return;
  }

  const response = await fetch("/api/runs/bulk-delete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ids })
  });
  const result = await response.json();
  selectedRunIds.clear();
  await loadRuns();
  refreshStatus.textContent = result.skipped.length === 0
    ? `Deleted ${result.deleted} run(s)`
    : `Deleted ${result.deleted}; skipped ${result.skipped.length}`;
});

for (const button of document.querySelectorAll("[data-sort]")) {
  button.addEventListener("click", () => {
    if (sortKey === button.dataset.sort) {
      sortDirection = sortDirection === "asc" ? "desc" : "asc";
    } else {
      sortKey = button.dataset.sort;
      sortDirection = sortKey === "name" || sortKey === "status" ? "asc" : "desc";
    }

    renderRuns();
  });
}

runSearch.addEventListener("input", renderRuns);
statusFilter.addEventListener("change", renderRuns);
scenarioFilter.addEventListener("change", renderRuns);
refreshButton.addEventListener("click", loadRuns);

async function renameRun(id) {
  const run = allRuns.find((candidate) => candidate.id === id);
  const name = prompt("Run name", run?.name || "");
  if (name === null) {
    return;
  }

  const response = await fetch(`/api/runs/${encodeURIComponent(id)}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name })
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Rename failed." }));
    refreshStatus.textContent = problem.error || "Rename failed.";
    return;
  }

  await loadRuns();
}

async function toggleRunDetails(id) {
  if (expandedRunId === id) {
    expandedRunId = null;
    renderRuns();
    return;
  }

  expandedRunId = id;
  if (!runDetailsById.has(id)) {
    runDetailsById.set(id, { loading: true });
    renderRuns();
    await loadRunDetails(id);
  } else {
    renderRuns();
  }
}

async function loadRunDetails(id) {
  const response = await fetch(`/api/runs/${encodeURIComponent(id)}/details?lines=80`);
  if (!response.ok) {
    runDetailsById.set(id, { loadError: "Details unavailable." });
    renderRuns();
    return;
  }

  const details = await response.json();
  runDetailsById.set(id, details);
  renderRuns();
}

function valueOrNull(selector) {
  const raw = document.querySelector(selector).value.trim();
  return raw === "" ? null : Number(raw);
}

function formatNumber(value) {
  return Number(value || 0).toLocaleString();
}

function formatSeed(value) {
  return value === null || value === undefined ? "pending" : Number(value).toLocaleString();
}

function formatDateTime(value) {
  return value ? new Date(value).toLocaleString() : "";
}

function formatLog(lines) {
  return Array.isArray(lines) && lines.length > 0 ? lines.join("\n") : "No output.";
}

function statusClass(value) {
  return escapeHtml(String(value || "unknown").toLowerCase().replace(/[^a-z0-9_-]+/g, "-"));
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll("\"", "&quot;")
    .replaceAll("'", "&#039;");
}

async function boot() {
  await loadScenarios();
  await loadRuns();
  refreshTimer = setInterval(loadRuns, 2000);
}

boot().catch((error) => {
  refreshStatus.textContent = "Error";
  formMessage.textContent = error.message;
});
