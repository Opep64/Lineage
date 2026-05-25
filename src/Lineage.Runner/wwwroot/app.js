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
const exportButton = document.querySelector("#exportButton");
const bulkDeleteButton = document.querySelector("#bulkDeleteButton");
const selectionStatus = document.querySelector("#selectionStatus");
const exportPanel = document.querySelector("#exportPanel");
const exportText = document.querySelector("#exportText");
const copyExportButton = document.querySelector("#copyExportButton");
const downloadExportButton = document.querySelector("#downloadExportButton");
const closeExportButton = document.querySelector("#closeExportButton");
const scenarioOptionsToggle = document.querySelector("#scenarioOptionsToggle");
const scenarioOptionsStatus = document.querySelector("#scenarioOptionsStatus");
const scenarioOptionsPanel = document.querySelector("#scenarioOptionsPanel");
const scenarioTabs = document.querySelector("#scenarioTabs");
const scenarioFields = document.querySelector("#scenarioFields");

let refreshTimer = null;
let allRuns = [];
let selectedRunIds = new Set();
let expandedRunId = null;
let runDetailsById = new Map();
let sortKey = "createdAtUtc";
let sortDirection = "desc";
let scenarioEditor = null;
let activeScenarioGroup = null;

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

  await loadScenarioEditor();
}

async function loadScenarioEditor() {
  scenarioEditor = null;
  activeScenarioGroup = null;
  scenarioTabs.innerHTML = "";
  scenarioFields.innerHTML = "";
  scenarioOptionsStatus.textContent = "Loading options...";

  if (!scenarioSelect.value) {
    scenarioOptionsStatus.textContent = "No scenario selected.";
    return;
  }

  const response = await fetch(`/api/scenario-editor?path=${encodeURIComponent(scenarioSelect.value)}`);
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Scenario options unavailable." }));
    scenarioOptionsStatus.textContent = problem.error || "Scenario options unavailable.";
    return;
  }

  scenarioEditor = await response.json();
  const groups = scenarioEditorGroups();
  activeScenarioGroup = groups[0] || null;
  renderScenarioEditor();
}

function renderScenarioEditor() {
  scenarioTabs.innerHTML = "";
  scenarioFields.innerHTML = "";

  if (!scenarioEditor) {
    return;
  }

  const groups = scenarioEditorGroups();
  if (!activeScenarioGroup || !groups.includes(activeScenarioGroup)) {
    activeScenarioGroup = groups[0] || null;
  }

  for (const group of groups) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `scenario-tab${group === activeScenarioGroup ? " is-active" : ""}`;
    button.textContent = group;
    button.dataset.group = group;
    button.setAttribute("role", "tab");
    button.setAttribute("aria-selected", String(group === activeScenarioGroup));
    scenarioTabs.append(button);
  }

  const activeFields = scenarioEditor.fields.filter((field) => field.group === activeScenarioGroup);
  for (const field of activeFields) {
    scenarioFields.append(createScenarioField(field));
  }

  scenarioOptionsStatus.textContent = `${scenarioEditor.fields.length} options`;
}

function scenarioEditorGroups() {
  if (!scenarioEditor) {
    return [];
  }

  const preferredOrder = [
    "Basics",
    "Brain & Vision",
    "World & Terrain",
    "Plants",
    "Seasons",
    "Reproduction",
    "Diet & Combat",
    "Energy & Movement",
    "Mutation",
    "Species",
    "Advanced"
  ];
  const groups = [...new Set(scenarioEditor.fields.map((field) => field.group))];
  return groups.sort((left, right) => {
    const leftIndex = preferredOrder.indexOf(left);
    const rightIndex = preferredOrder.indexOf(right);
    if (leftIndex !== -1 || rightIndex !== -1) {
      return (leftIndex === -1 ? Number.MAX_SAFE_INTEGER : leftIndex)
        - (rightIndex === -1 ? Number.MAX_SAFE_INTEGER : rightIndex);
    }

    return left.localeCompare(right);
  });
}

function createScenarioField(field) {
  const wrapper = document.createElement("label");
  wrapper.className = `scenario-field scenario-field-${field.type}`;

  const label = document.createElement("span");
  label.className = "scenario-field-label";
  label.textContent = field.label;
  wrapper.append(label);

  wrapper.append(createScenarioControl(field));
  return wrapper;
}

function createScenarioControl(field) {
  const value = scenarioEditor.scenario?.[field.jsonName];
  let control;

  if (field.type === "boolean") {
    control = document.createElement("input");
    control.type = "checkbox";
    control.checked = Boolean(value);
  } else if (field.type === "enum") {
    control = document.createElement("select");
    for (const enumValue of field.enumValues) {
      const option = document.createElement("option");
      option.value = enumValue;
      option.textContent = enumValue;
      control.append(option);
    }

    control.value = value ?? "";
  } else if (field.type === "json") {
    control = document.createElement("textarea");
    control.rows = 8;
    control.spellcheck = false;
    control.value = JSON.stringify(value ?? null, null, 2);
  } else {
    control = document.createElement("input");
    control.type = field.type === "number" ? "number" : "text";
    if (field.type === "number") {
      control.step = "any";
    }

    control.value = value ?? "";
  }

  control.className = "scenario-control";
  control.dataset.jsonName = field.jsonName;
  control.dataset.type = field.type;
  return control;
}

function collectScenarioOptions() {
  if (!scenarioEditor) {
    return null;
  }

  storeVisibleScenarioValues();
  return JSON.parse(JSON.stringify(scenarioEditor.scenario));
}

function storeVisibleScenarioValues() {
  if (!scenarioEditor) {
    return;
  }

  for (const control of scenarioFields.querySelectorAll(".scenario-control")) {
    const field = scenarioEditor.fields.find((candidate) => candidate.jsonName === control.dataset.jsonName);
    if (!field) {
      continue;
    }

    scenarioEditor.scenario[field.jsonName] = readScenarioControlValue(control, field);
  }
}

function readScenarioControlValue(control, field) {
  if (field.type === "boolean") {
    return control.checked;
  }

  if (field.type === "number") {
    const raw = control.value.trim();
    if (raw === "") {
      throw new Error(`${field.label} needs a numeric value.`);
    }

    const parsed = Number(raw);
    if (!Number.isFinite(parsed)) {
      throw new Error(`${field.label} needs a numeric value.`);
    }

    return parsed;
  }

  if (field.type === "json") {
    try {
      return JSON.parse(control.value);
    } catch {
      throw new Error(`${field.label} needs valid JSON.`);
    }
  }

  return control.value;
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
        ${run.scenarioSummary ? `<div class="run-sub">brain ${escapeHtml(formatScenarioInline(run.scenarioSummary))}</div>` : ""}
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
          <div><span>Brain</span><strong>${escapeHtml(formatScenarioBrain(details.run.scenarioSummary))}</strong></div>
          <div><span>Vision</span><strong>${escapeHtml(formatScenarioVision(details.run.scenarioSummary))}</strong></div>
          <div><span>World</span><strong>${escapeHtml(formatScenarioWorld(details.run.scenarioSummary))}</strong></div>
          <div><span>Resources</span><strong>${escapeHtml(formatScenarioResources(details.run.scenarioSummary))}</strong></div>
          <div><span>Stats</span><strong>${escapeHtml(details.run.statsPath)}</strong></div>
          <div><span>Snapshot</span><strong>${escapeHtml(details.run.snapshotPath)}</strong></div>
          <div><span>Launch scenario</span><strong>${escapeHtml(details.run.launchScenarioPath || "")}</strong></div>
          <div><span>Resolved scenario</span><strong>${escapeHtml(details.run.resolvedScenarioPath)}</strong></div>
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
      formatScenarioInline(run.scenarioSummary),
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
  exportButton.disabled = selectedRunIds.size === 0;
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
  let scenario = null;
  try {
    scenario = collectScenarioOptions();
  } catch (error) {
    formMessage.textContent = error.message;
    return;
  }

  const checkpointInterval = valueOrNull("#checkpointInterval");
  const payload = {
    scenarioPath: scenarioSelect.value,
    scenario,
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

exportButton.addEventListener("click", async () => {
  const ids = [...selectedRunIds];
  if (ids.length === 0) {
    return;
  }

  exportButton.disabled = true;
  refreshStatus.textContent = "Exporting";
  try {
    const response = await fetch("/api/runs/export", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ ids })
    });

    if (!response.ok) {
      refreshStatus.textContent = "Export failed";
      return;
    }

    exportText.value = await response.text();
    exportPanel.hidden = false;
    refreshStatus.textContent = `Exported ${ids.length} run(s)`;
  } finally {
    updateSelectionControls();
  }
});

copyExportButton.addEventListener("click", async () => {
  if (!exportText.value) {
    return;
  }

  try {
    await navigator.clipboard.writeText(exportText.value);
  } catch {
    exportText.select();
    document.execCommand("copy");
  }

  refreshStatus.textContent = "Export copied";
});

downloadExportButton.addEventListener("click", () => {
  if (!exportText.value) {
    return;
  }

  const downloadUrl = URL.createObjectURL(new Blob([exportText.value], { type: "text/markdown" }));
  const link = document.createElement("a");
  link.href = downloadUrl;
  link.download = `lineage-run-export-${formatDownloadTimestamp(new Date())}.md`;
  document.body.append(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(downloadUrl);
  refreshStatus.textContent = "Export downloaded";
});

closeExportButton.addEventListener("click", () => {
  exportPanel.hidden = true;
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

scenarioSelect.addEventListener("change", loadScenarioEditor);

scenarioOptionsToggle.addEventListener("click", () => {
  scenarioOptionsPanel.hidden = !scenarioOptionsPanel.hidden;
});

scenarioTabs.addEventListener("click", (event) => {
  const button = event.target.closest("button[data-group]");
  if (!button || button.dataset.group === activeScenarioGroup) {
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
    return;
  }

  activeScenarioGroup = button.dataset.group;
  renderScenarioEditor();
});

scenarioFields.addEventListener("change", () => {
  try {
    storeVisibleScenarioValues();
    scenarioOptionsStatus.textContent = `${scenarioEditor?.fields.length ?? 0} options`;
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
  }
});

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

function formatDownloadTimestamp(value) {
  return value.toISOString().slice(0, 19).replaceAll(":", "").replace("T", "-");
}

function formatScenarioInline(summary) {
  if (!summary) {
    return "";
  }

  return [
    formatScenarioBrain(summary),
    formatScenarioWorld(summary),
    formatScenarioResources(summary)
  ].filter(Boolean).join(" | ");
}

function formatScenarioBrain(summary) {
  if (!summary) {
    return "";
  }

  const hidden = summary.brainHiddenNodeCount === null || summary.brainHiddenNodeCount === undefined
    ? ""
    : `, hidden ${formatNumber(summary.brainHiddenNodeCount)}`;
  return [summary.brainArchitectureKind, summary.initialBrainKind].filter(Boolean).join(" / ") + hidden;
}

function formatScenarioVision(summary) {
  if (!summary) {
    return "";
  }

  const bits = [];
  if (summary.enableSectorVision !== null && summary.enableSectorVision !== undefined) {
    bits.push(`sector ${formatOnOff(summary.enableSectorVision)}`);
  }

  if (summary.enableLegacyNearestFoodVisionInputs !== null && summary.enableLegacyNearestFoodVisionInputs !== undefined) {
    bits.push(`legacy food ${formatOnOff(summary.enableLegacyNearestFoodVisionInputs)}`);
  }

  if (summary.enableLegacyNearestCreatureVisionInputs !== null && summary.enableLegacyNearestCreatureVisionInputs !== undefined) {
    bits.push(`legacy creature ${formatOnOff(summary.enableLegacyNearestCreatureVisionInputs)}`);
  }

  if (summary.visionAngleDegrees !== null && summary.visionAngleDegrees !== undefined) {
    bits.push(`${formatDecimal(summary.visionAngleDegrees)} deg`);
  }

  return bits.join(", ");
}

function formatScenarioWorld(summary) {
  if (!summary || summary.worldWidth === null || summary.worldWidth === undefined || summary.worldHeight === null || summary.worldHeight === undefined) {
    return "";
  }

  return `${formatDecimal(summary.worldWidth)} x ${formatDecimal(summary.worldHeight)}`;
}

function formatScenarioResources(summary) {
  if (!summary) {
    return "";
  }

  const bits = [];
  if (summary.initialResourcesPerMillionArea !== null && summary.initialResourcesPerMillionArea !== undefined) {
    bits.push(`${formatDecimal(summary.initialResourcesPerMillionArea)}/M plants`);
  }

  if (summary.initialResourceCount !== null && summary.initialResourceCount !== undefined) {
    bits.push(`${formatNumber(summary.initialResourceCount)} initial`);
  }

  if (summary.initialCreatureCount !== null && summary.initialCreatureCount !== undefined) {
    bits.push(`${formatNumber(summary.initialCreatureCount)} creatures`);
  }

  return bits.join(", ");
}

function formatDecimal(value) {
  return Number(value || 0).toLocaleString(undefined, { maximumFractionDigits: 3 });
}

function formatOnOff(value) {
  return value ? "on" : "off";
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
