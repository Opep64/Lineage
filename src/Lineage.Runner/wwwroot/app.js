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
const scenarioOptionSearch = document.querySelector("#scenarioOptionSearch");
const scenarioScopeInputs = [...document.querySelectorAll("input[name='scenarioOptionScope']")];
const resetScenarioGroupButton = document.querySelector("#resetScenarioGroupButton");
const resetScenarioAllButton = document.querySelector("#resetScenarioAllButton");
const saveScenarioButton = document.querySelector("#saveScenarioButton");
const deleteScenarioButton = document.querySelector("#deleteScenarioButton");
const recipePicker = document.querySelector("#recipePicker");
const recipeOptions = document.querySelector("#recipeOptions");
const applyRecipeButton = document.querySelector("#applyRecipeButton");
const archiveRecipeButton = document.querySelector("#archiveRecipeButton");
const deleteRecipeButton = document.querySelector("#deleteRecipeButton");
const reviewLaunchDiffButton = document.querySelector("#reviewLaunchDiffButton");
const saveRecipeButton = document.querySelector("#saveRecipeButton");
const saveNewRecipeButton = document.querySelector("#saveNewRecipeButton");
const recipeStack = document.querySelector("#recipeStack");
const scenarioDiffPanel = document.querySelector("#scenarioDiffPanel");
const scenarioDiffTitle = document.querySelector("#scenarioDiffTitle");
const scenarioDiffSummary = document.querySelector("#scenarioDiffSummary");
const scenarioDiffBody = document.querySelector("#scenarioDiffBody");
const confirmSaveRecipeButton = document.querySelector("#confirmSaveRecipeButton");
const closeScenarioDiffButton = document.querySelector("#closeScenarioDiffButton");
const seedInput = document.querySelector("#seed");
const biomePreview = document.querySelector("#biomePreview");
const biomePreviewContent = document.querySelector("#biomePreviewContent");
const biomePreviewCanvas = document.querySelector("#biomePreviewCanvas");
const biomePreviewMeta = document.querySelector("#biomePreviewMeta");
const biomePreviewLegend = document.querySelector("#biomePreviewLegend");
const biomePreviewStatus = document.querySelector("#biomePreviewStatus");
const toggleBiomePreviewButton = document.querySelector("#toggleBiomePreviewButton");
const refreshBiomePreviewButton = document.querySelector("#refreshBiomePreviewButton");
const paintLayerSelect = document.querySelector("#paintLayerSelect");
const biomeBrushSelect = document.querySelector("#biomeBrushSelect");
const obstacleBrushSelect = document.querySelector("#obstacleBrushSelect");
const mapArtifactSelect = document.querySelector("#mapArtifactSelect");
const applyMapArtifactButton = document.querySelector("#applyMapArtifactButton");
const saveMapArtifactButton = document.querySelector("#saveMapArtifactButton");
const renameMapArtifactButton = document.querySelector("#renameMapArtifactButton");
const duplicateMapArtifactButton = document.querySelector("#duplicateMapArtifactButton");
const deleteMapArtifactButton = document.querySelector("#deleteMapArtifactButton");
const mapArtifactDetails = document.querySelector("#mapArtifactDetails");
const paintBiomeMapButton = document.querySelector("#paintBiomeMapButton");
const brainCatalogSelect = document.querySelector("#brainCatalogSelect");
const brainCatalogDetails = document.querySelector("#brainCatalogDetails");
const refreshBrainCatalogButton = document.querySelector("#refreshBrainCatalogButton");
const deleteBrainCatalogButton = document.querySelector("#deleteBrainCatalogButton");
const brainLabSnapshotSelect = document.querySelector("#brainLabSnapshotSelect");
const brainLabSnapshotPath = document.querySelector("#brainLabSnapshotPath");
const brainLabCreatureSelect = document.querySelector("#brainLabCreatureSelect");
const brainLabGroupFilter = document.querySelector("#brainLabGroupFilter");
const refreshBrainLabSnapshotsButton = document.querySelector("#refreshBrainLabSnapshotsButton");
const loadBrainLabSnapshotButton = document.querySelector("#loadBrainLabSnapshotButton");
const evaluateBrainLabButton = document.querySelector("#evaluateBrainLabButton");
const muteBrainLabSoundButton = document.querySelector("#muteBrainLabSoundButton");
const resetBrainLabOverridesButton = document.querySelector("#resetBrainLabOverridesButton");
const brainLabMeta = document.querySelector("#brainLabMeta");
const brainLabInputs = document.querySelector("#brainLabInputs");
const brainLabOutputs = document.querySelector("#brainLabOutputs");
const brainLabStatus = document.querySelector("#brainLabStatus");
const speciesCatalogSelect = document.querySelector("#speciesCatalogSelect");
const speciesCatalogDetails = document.querySelector("#speciesCatalogDetails");
const speciesRosterDetails = document.querySelector("#speciesRosterDetails");
const refreshSpeciesCatalogButton = document.querySelector("#refreshSpeciesCatalogButton");
const deleteSpeciesCatalogButton = document.querySelector("#deleteSpeciesCatalogButton");
const addSpeciesToScenarioButton = document.querySelector("#addSpeciesToScenarioButton");
const speciesSeedBrainSelect = document.querySelector("#speciesSeedBrain");
const speciesSeedCountInput = document.querySelector("#speciesSeedCount");
const speciesSeedRegionSelect = document.querySelector("#speciesSeedRegion");
const speciesSeedEnergyInput = document.querySelector("#speciesSeedEnergy");

let refreshTimer = null;
let allRuns = [];
let scenarioOptions = [];
let mapArtifacts = [];
let scenarioRecipes = [];
let brainCatalog = [];
let speciesCatalog = [];
let brainLabSnapshots = [];
let brainLabSnapshot = null;
let brainLabEvaluation = null;
let brainLabOverrides = {};
let brainLabEvaluateTimer = null;
let appliedRecipes = [];
let recipeBaseScenario = null;
let recipeDiffCheckpoint = null;
let pendingRecipeSave = null;
let selectedRunIds = new Set();

const speciesGeneratedBrainChoices = [
  ["scenario", "Scenario initial brain"],
  ["seedForager", "Seed forager"],
  ["explorerForager", "Explorer forager"],
  ["sectorForager", "Sector forager"],
  ["opportunisticForager", "Opportunistic forager"],
  ["scavengerForager", "Scavenger forager"],
  ["freshnessAwareScavenger", "Freshness-aware scavenger"],
  ["foragerPredator", "Forager predator"],
  ["randomPerFounder", "Random per founder"]
];
let expandedRunId = null;
let runDetailsById = new Map();
let sortKey = "createdAtUtc";
let sortDirection = "desc";
let scenarioEditor = null;
let scenarioEditorBaseline = null;
let activeScenarioGroup = null;
let biomePreviewTimer = null;
let biomePreviewRequestId = 0;
let currentBiomePreview = null;
let biomePreviewCollapsed = false;
let biomePaintEnabled = false;
let biomePaintDirty = false;
let biomePaintPointerDown = false;

async function loadScenarios(selectedPath = scenarioSelect.value) {
  const response = await fetch("/api/scenarios");
  scenarioOptions = await response.json();
  scenarioSelect.innerHTML = "";
  for (const scenario of scenarioOptions) {
    const option = document.createElement("option");
    option.value = scenario.path;
    option.textContent = scenarioOptionLabel(scenario);
    option.dataset.isUserCreated = String(Boolean(scenario.isUserCreated));
    option.dataset.canDelete = String(Boolean(scenario.canDelete));
    scenarioSelect.append(option);
  }

  if ([...scenarioSelect.options].some((option) => option.value === selectedPath)) {
    scenarioSelect.value = selectedPath;
  } else if (scenarioSelect.options.length > 0) {
    scenarioSelect.selectedIndex = 0;
  }

  await loadScenarioEditor();
}

async function loadMapArtifacts(selectedPath = mapArtifactSelect?.value || "") {
  const response = await fetch("/api/map-artifacts");
  mapArtifacts = response.ok ? await response.json() : [];
  renderMapArtifactOptions(selectedPath);
  renderMapArtifactDetails();
  updateBiomePaintControls();
}

async function loadSpeciesCatalog(selectedPath = speciesCatalogSelect?.value || "") {
  const response = await fetch("/api/species-catalog");
  speciesCatalog = response.ok ? await response.json() : [];
  renderSpeciesCatalogOptions(selectedPath);
  renderSpeciesCatalogDetails();
  renderSpeciesRoster();
}

async function loadBrainCatalog(selectedPath = brainCatalogSelect?.value || "") {
  const response = await fetch("/api/brain-catalog");
  brainCatalog = response.ok ? await response.json() : [];
  renderBrainCatalogOptions(selectedPath);
  renderBrainCatalogDetails();
  renderSpeciesBrainChoiceOptions();
  renderSpeciesCatalogDetails();
  renderSpeciesRoster();
}

function renderBrainCatalogOptions(selectedPath = "") {
  if (!brainCatalogSelect) {
    return;
  }

  brainCatalogSelect.innerHTML = "";
  const empty = document.createElement("option");
  empty.value = "";
  empty.textContent = brainCatalog.length > 0 ? "Choose brain profile" : "No brain profiles";
  brainCatalogSelect.append(empty);

  for (const brain of brainCatalog) {
    const option = document.createElement("option");
    option.value = brain.path;
    option.textContent = `${brain.name} (${brain.path})${brain.isCompatible === false ? " [incompatible]" : ""}`;
    option.title = [
      `${brain.brainArchitectureKind}, hidden ${formatNumber(brain.hiddenNodeCount)}`,
      `${formatNumber(brain.weightCount)} weights`,
      brain.compatibilityStatus || null,
      brain.sourceScenarioName ? `source ${brain.sourceScenarioName}` : null
    ].filter(Boolean).join(" | ");
    brainCatalogSelect.append(option);
  }

  brainCatalogSelect.value = [...brainCatalogSelect.options].some((option) => option.value === selectedPath)
    ? selectedPath
    : "";
  updateBrainCatalogButtons();
}

function renderSpeciesBrainChoiceOptions(selectedValue = speciesSeedBrainSelect?.value || "profile") {
  if (!speciesSeedBrainSelect) {
    return;
  }

  speciesSeedBrainSelect.innerHTML = brainChoiceOptionsHtml(selectedValue, null, true);
  if (![...speciesSeedBrainSelect.options].some((option) => option.value === selectedValue && !option.disabled)) {
    speciesSeedBrainSelect.value = "profile";
  }
}

function brainChoiceOptionsHtml(selectedValue = "profile", missingBrainPath = null, includeScenario = false) {
  const selected = selectedValue || "profile";
  const pieces = [
    `<option value="profile"${selected === "profile" ? " selected" : ""}>Profile/default brain</option>`
  ];

  pieces.push(`<optgroup label="Generated starter brain">`);
  for (const [kind, label] of speciesGeneratedBrainChoices) {
    if (!includeScenario && kind === "scenario") {
      continue;
    }

    const value = `generated:${kind}`;
    pieces.push(`<option value="${escapeHtml(value)}"${selected === value ? " selected" : ""}>${escapeHtml(label)}</option>`);
  }
  pieces.push(`</optgroup>`);

  if (brainCatalog.length > 0 || missingBrainPath) {
    pieces.push(`<optgroup label="Catalog brain profile">`);
    for (const brain of brainCatalog) {
      const value = `catalog:${brain.path}`;
      pieces.push([
        `<option value="${escapeHtml(value)}"`,
        selected === value ? " selected" : "",
        brain.isCompatible === false ? " disabled" : "",
        ` title="${escapeHtml(`${brain.path} | hidden ${formatNumber(brain.hiddenNodeCount)} | ${formatNumber(brain.weightCount)} weights | ${brainCompatibilityStatus(brain)}`)}">`,
        `${escapeHtml(brain.name)} (${escapeHtml(brain.brainArchitectureKind)})${brain.isCompatible === false ? " [incompatible]" : ""}`,
        `</option>`
      ].join(""));
    }

    if (missingBrainPath && !findBrainCatalogEntryByPath(missingBrainPath)) {
      const value = `catalog:${missingBrainPath}`;
      pieces.push(`<option value="${escapeHtml(value)}"${selected === value ? " selected" : ""}>Missing catalog brain: ${escapeHtml(missingBrainPath)}</option>`);
    }
    pieces.push(`</optgroup>`);
  }

  return pieces.join("");
}

function selectedBrainCatalogEntry() {
  return brainCatalog.find((candidate) => candidate.path === brainCatalogSelect?.value) ?? null;
}

function findBrainCatalogEntryByPath(path) {
  return brainCatalog.find((candidate) => candidate.path === path) ?? null;
}

function brainCompatibilityStatus(brain) {
  if (!brain) {
    return "No catalog brain profile found.";
  }

  return brain.compatibilityStatus || (brain.isCompatible === false
    ? "Brain profile is not compatible with the current runtime."
    : "Compatible with the current sense/action schema.");
}

function renderBrainCompatibilityWarnings(brain) {
  const warnings = Array.isArray(brain?.compatibilityWarnings)
    ? brain.compatibilityWarnings.filter(Boolean)
    : [];
  if (warnings.length === 0) {
    return "";
  }

  return `
    <div class="map-artifact-compatibility ${brain.isCompatible === false ? "map-artifact-warning" : "map-artifact-ok"}">
      <ul>${warnings.map((warning) => `<li>${escapeHtml(warning)}</li>`).join("")}</ul>
    </div>
  `;
}

function renderBrainCatalogDetails() {
  if (!brainCatalogDetails) {
    return;
  }

  const brain = selectedBrainCatalogEntry();
  updateBrainCatalogButtons();
  if (!brain) {
    brainCatalogDetails.textContent = "Choose a brain profile to inspect it.";
    return;
  }

  brainCatalogDetails.innerHTML = `
    <div class="species-summary-grid">
      <div><span>Name</span><strong>${escapeHtml(brain.name)}</strong></div>
      <div><span>Path</span><strong>${escapeHtml(brain.path)}</strong></div>
      <div><span>Architecture</span><strong>${escapeHtml(brain.brainArchitectureKind)}, hidden ${formatNumber(brain.hiddenNodeCount)}</strong></div>
      <div><span>Weights</span><strong>${formatNumber(brain.weightCount)}</strong></div>
      <div><span>Schema</span><strong>input v${formatNumber(brain.inputSchemaVersion)} (${formatNumber(brain.inputCount)}), output v${formatNumber(brain.outputSchemaVersion)} (${formatNumber(brain.outputCount)})</strong></div>
      <div><span>Compatibility</span><strong class="${brain.isCompatible === false ? "map-artifact-warning" : "map-artifact-ok"}">${escapeHtml(brainCompatibilityStatus(brain))}</strong></div>
      <div><span>Source</span><strong>${escapeHtml(formatBrainSource(brain))}</strong></div>
    </div>
    ${brain.notes ? `<div class="species-notes">${escapeHtml(brain.notes)}</div>` : ""}
    ${renderBrainCompatibilityWarnings(brain)}
  `;
}

function updateBrainCatalogButtons() {
  const brain = selectedBrainCatalogEntry();
  if (deleteBrainCatalogButton) {
    deleteBrainCatalogButton.disabled = !brain?.canDelete;
  }
}

async function loadBrainLabSnapshots(selectedPath = brainLabSelectedPath()) {
  if (!brainLabSnapshotSelect) {
    return;
  }

  const response = await fetch("/api/brain-lab/snapshots");
  brainLabSnapshots = response.ok ? await response.json() : [];
  renderBrainLabSnapshotOptions(selectedPath);
}

function renderBrainLabSnapshotOptions(selectedPath = "") {
  if (!brainLabSnapshotSelect) {
    return;
  }

  brainLabSnapshotSelect.innerHTML = "";
  const empty = document.createElement("option");
  empty.value = "";
  empty.textContent = brainLabSnapshots.length > 0 ? "Choose snapshot" : "No snapshots found";
  brainLabSnapshotSelect.append(empty);

  for (const snapshot of brainLabSnapshots) {
    const option = document.createElement("option");
    option.value = snapshot.path;
    option.textContent = `${snapshot.path}${snapshot.sizeBytes ? ` (${formatFileSize(snapshot.sizeBytes)})` : ""}`;
    option.title = formatDateTime(snapshot.modifiedAtUtc);
    brainLabSnapshotSelect.append(option);
  }

  const selected = [...brainLabSnapshotSelect.options].some((option) => option.value === selectedPath)
    ? selectedPath
    : brainLabSnapshots[0]?.path || "";
  brainLabSnapshotSelect.value = selected;
  if (selected && brainLabSnapshotPath && !brainLabSnapshotPath.value.trim()) {
    brainLabSnapshotPath.value = selected;
  }
}

async function loadBrainLabSnapshot() {
  const path = brainLabSelectedPath();
  if (!path) {
    brainLabStatus.textContent = "Choose a snapshot.";
    return;
  }

  brainLabStatus.textContent = "Loading snapshot";
  const response = await fetch(`/api/brain-lab/snapshot?path=${encodeURIComponent(path)}`);
  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Snapshot load failed.");
    return;
  }

  brainLabSnapshot = await response.json();
  brainLabEvaluation = null;
  brainLabOverrides = {};
  if (brainLabSnapshotPath) {
    brainLabSnapshotPath.value = brainLabSnapshot.path;
  }
  if (brainLabSnapshotSelect && [...brainLabSnapshotSelect.options].some((option) => option.value === brainLabSnapshot.path)) {
    brainLabSnapshotSelect.value = brainLabSnapshot.path;
  }

  renderBrainLabSnapshot();
  brainLabStatus.textContent = `Loaded ${brainLabSnapshot.path}`;
  if (brainLabSnapshot.creatures?.length > 0) {
    await evaluateBrainLab();
  }
}

function renderBrainLabSnapshot() {
  renderBrainLabMeta();
  renderBrainLabCreatureOptions();
  renderBrainLabInputs();
  renderBrainLabOutputs();
  updateBrainLabButtons();
}

function renderBrainLabCreatureOptions() {
  if (!brainLabCreatureSelect) {
    return;
  }

  brainLabCreatureSelect.innerHTML = "";
  const creatures = brainLabSnapshot?.creatures || [];
  if (creatures.length === 0) {
    const option = document.createElement("option");
    option.value = "";
    option.textContent = "No creatures";
    brainLabCreatureSelect.append(option);
    brainLabCreatureSelect.disabled = true;
    return;
  }

  for (const creature of creatures) {
    const option = document.createElement("option");
    option.value = String(creature.id);
    option.textContent = `#${formatNumber(creature.id)} gen ${formatNumber(creature.generation)} ${creature.brainArchitectureKind}`;
    option.title = [
      `energy ${formatBrainLabNumber(creature.energyRatio)}`,
      `health ${formatBrainLabNumber(creature.healthRatio)}`,
      `sound ${formatBrainLabNumber(creature.soundDensity)}`
    ].join(" | ");
    brainLabCreatureSelect.append(option);
  }

  brainLabCreatureSelect.disabled = false;
}

async function evaluateBrainLab() {
  const path = brainLabSelectedPath();
  const creatureId = Number(brainLabCreatureSelect?.value || 0);
  if (!path || !creatureId) {
    updateBrainLabButtons();
    return;
  }

  if (brainLabEvaluateTimer) {
    clearTimeout(brainLabEvaluateTimer);
    brainLabEvaluateTimer = null;
  }

  brainLabStatus.textContent = "Evaluating";
  const response = await fetch("/api/brain-lab/evaluate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      creatureId,
      inputOverrides: brainLabOverrides
    })
  });

  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Evaluation failed.");
    return;
  }

  brainLabEvaluation = await response.json();
  renderBrainLabMeta();
  renderBrainLabInputs();
  renderBrainLabOutputs();
  updateBrainLabButtons();
  brainLabStatus.textContent = [
    `${formatNumber(brainLabEvaluation.changedOutputCount)} changed outputs`,
    `${formatNumber(brainLabEvaluation.gateFlipCount)} gate flips`,
    `max delta ${formatBrainLabNumber(brainLabEvaluation.maxAbsoluteOutputDelta)}`,
    brainLabEvaluation.supportsRawInputOverrides === false ? "raw overrides unavailable for rtNEAT" : null
  ].filter(Boolean).join(" | ");
}

function scheduleBrainLabEvaluate(delay = 160) {
  if (brainLabEvaluateTimer) {
    clearTimeout(brainLabEvaluateTimer);
  }

  brainLabEvaluateTimer = setTimeout(() => {
    brainLabEvaluateTimer = null;
    evaluateBrainLab();
  }, delay);
}

function renderBrainLabMeta() {
  if (!brainLabMeta) {
    return;
  }

  if (!brainLabSnapshot) {
    brainLabMeta.textContent = "No snapshot loaded.";
    return;
  }

  const creature = brainLabEvaluation?.creature;
  const creatureText = creature
    ? ` | creature #${formatNumber(creature.id)} gen ${formatNumber(creature.generation)} ${creature.brainArchitectureKind}`
    : "";
  brainLabMeta.textContent = [
    brainLabSnapshot.scenarioName || "snapshot",
    `seed ${formatSeed(brainLabSnapshot.seed)}`,
    `tick ${formatNumber(brainLabSnapshot.tick)}`,
    `${formatNumber(brainLabSnapshot.creatureCount)} creatures${brainLabSnapshot.creatureListTruncated ? ` (${formatNumber(brainLabSnapshot.returnedCreatureCount)} shown)` : ""}`
  ].join(" | ") + creatureText;
}

function renderBrainLabInputs() {
  if (!brainLabInputs) {
    return;
  }

  const inputs = brainLabEvaluation?.inputs || [];
  if (inputs.length === 0) {
    brainLabInputs.innerHTML = `<div class="empty">No inputs.</div>`;
    return;
  }

  const group = brainLabGroupFilter?.value || "Sound";
  const visible = inputs.filter((input) => group === "all" ? input.group !== "Bias" : input.group === group);
  if (visible.length === 0) {
    brainLabInputs.innerHTML = `<div class="empty">No ${escapeHtml(group)} inputs.</div>`;
    return;
  }

  brainLabInputs.innerHTML = visible.map(renderBrainLabInput).join("");
}

function renderBrainLabInput(input) {
  const min = Number(input.minimumValue);
  const max = Number(input.maximumValue);
  const value = Number(input.modifiedValue);
  const disabled = min === max || brainLabEvaluation?.supportsRawInputOverrides === false ? " disabled" : "";
  const step = max - min <= 2 ? "0.01" : "0.05";
  const overrideClass = input.overridden ? " is-overridden" : "";
  return `
    <div class="brain-lab-input-row${overrideClass}" title="${escapeHtml(input.meaning)}">
      <div class="brain-lab-input-heading">
        <span>${escapeHtml(input.name)}</span>
        <code>${escapeHtml(input.key)}</code>
      </div>
      <div class="brain-lab-input-values">
        <span>base ${formatBrainLabNumber(input.baselineValue)}</span>
        <span>edit ${formatBrainLabNumber(input.modifiedValue)}</span>
      </div>
      <div class="brain-lab-input-controls">
        <input data-brain-lab-input-key="${escapeHtml(input.key)}" type="range" min="${min}" max="${max}" step="${step}" value="${formatBrainLabControlValue(value)}"${disabled}>
        <input data-brain-lab-input-key="${escapeHtml(input.key)}" type="number" min="${min}" max="${max}" step="${step}" value="${formatBrainLabControlValue(value)}"${disabled}>
        <button class="secondary" data-brain-lab-reset-input="${escapeHtml(input.key)}" type="button"${input.overridden ? "" : " disabled"}>Base</button>
      </div>
    </div>
  `;
}

function renderBrainLabOutputs() {
  if (!brainLabOutputs) {
    return;
  }

  const outputs = brainLabEvaluation?.outputs || [];
  if (outputs.length === 0) {
    brainLabOutputs.textContent = "No evaluation yet.";
    return;
  }

  brainLabOutputs.innerHTML = `
    <div class="brain-lab-output-summary">
      <strong>${formatNumber(brainLabEvaluation.overrideCount)} overrides</strong>
      <span>${formatNumber(brainLabEvaluation.changedOutputCount)} changed</span>
      <span>${formatNumber(brainLabEvaluation.gateFlipCount)} gate flips</span>
    </div>
    <table class="brain-lab-output-table">
      <thead>
        <tr>
          <th>Output</th>
          <th>Base</th>
          <th>Edit</th>
          <th>Delta</th>
          <th>Gate</th>
        </tr>
      </thead>
      <tbody>
        ${outputs.map(renderBrainLabOutputRow).join("")}
      </tbody>
    </table>
  `;
}

function renderBrainLabOutputRow(output) {
  const deltaClass = output.delta > 0 ? "is-positive" : output.delta < 0 ? "is-negative" : "";
  const gate = output.activationThreshold === null || output.activationThreshold === undefined
    ? ""
    : `${output.baselineActive ? "on" : "off"} -> ${output.modifiedActive ? "on" : "off"}`;
  return `
    <tr class="${output.changed ? "is-changed" : ""}">
      <td><strong>${escapeHtml(output.name)}</strong><code>${escapeHtml(output.key)}</code></td>
      <td>${formatBrainLabNumber(output.baselineValue)}</td>
      <td>${formatBrainLabNumber(output.modifiedValue)}</td>
      <td class="${deltaClass}">${formatBrainLabDelta(output.delta)}</td>
      <td>${escapeHtml(gate)}</td>
    </tr>
  `;
}

function updateBrainLabInputOverride(control) {
  if (brainLabEvaluation?.supportsRawInputOverrides === false) {
    return;
  }

  const key = control.dataset.brainLabInputKey;
  const input = brainLabEvaluation?.inputs?.find((candidate) => candidate.key === key);
  if (!input) {
    return;
  }

  const rawValue = Number(control.value);
  if (!Number.isFinite(rawValue)) {
    return;
  }

  const clamped = Math.min(Math.max(rawValue, Number(input.minimumValue)), Number(input.maximumValue));
  if (Math.abs(clamped - Number(input.baselineValue)) <= 0.0005) {
    delete brainLabOverrides[key];
  } else {
    brainLabOverrides[key] = clamped;
  }

  syncBrainLabInputControls(key, clamped);
  scheduleBrainLabEvaluate();
}

function resetBrainLabInput(key) {
  const input = brainLabEvaluation?.inputs?.find((candidate) => candidate.key === key);
  if (!input) {
    return;
  }

  delete brainLabOverrides[key];
  syncBrainLabInputControls(key, Number(input.baselineValue));
  scheduleBrainLabEvaluate();
}

function syncBrainLabInputControls(key, value) {
  if (!brainLabInputs) {
    return;
  }

  for (const control of brainLabInputs.querySelectorAll("[data-brain-lab-input-key]")) {
    if (control.dataset.brainLabInputKey === key) {
      control.value = formatBrainLabControlValue(value);
    }
  }
}

function muteBrainLabSound() {
  if (brainLabEvaluation?.supportsRawInputOverrides === false) {
    return;
  }

  const inputs = brainLabEvaluation?.inputs || [];
  for (const input of inputs.filter((candidate) => candidate.group === "Sound")) {
    brainLabOverrides[input.key] = Number(input.neutralValue);
  }
  evaluateBrainLab();
}

function resetBrainLabOverrides() {
  brainLabOverrides = {};
  evaluateBrainLab();
}

function updateBrainLabButtons() {
  const hasSnapshot = Boolean(brainLabSnapshot);
  const hasCreature = Boolean(brainLabCreatureSelect?.value);
  const hasEvaluation = Boolean(brainLabEvaluation);
  const supportsOverrides = brainLabEvaluation?.supportsRawInputOverrides !== false;
  if (evaluateBrainLabButton) {
    evaluateBrainLabButton.disabled = !hasSnapshot || !hasCreature;
  }
  if (muteBrainLabSoundButton) {
    muteBrainLabSoundButton.disabled = !hasEvaluation || !supportsOverrides;
  }
  if (resetBrainLabOverridesButton) {
    resetBrainLabOverridesButton.disabled = !hasEvaluation || !supportsOverrides || Object.keys(brainLabOverrides).length === 0;
  }
}

function brainLabSelectedPath() {
  return (brainLabSnapshotPath?.value || brainLabSnapshotSelect?.value || "").trim();
}

function formatBrainLabNumber(value) {
  return Number(value || 0).toLocaleString(undefined, { maximumFractionDigits: 3 });
}

function formatBrainLabDelta(value) {
  const number = Number(value || 0);
  const formatted = formatBrainLabNumber(number);
  return number > 0 ? `+${formatted}` : formatted;
}

function formatBrainLabControlValue(value) {
  return Number(value || 0).toFixed(4).replace(/\.?0+$/, "");
}

function formatBrainSource(brain) {
  const bits = [];
  if (brain.sourceScenarioName) {
    bits.push(brain.sourceScenarioName);
  }

  if (brain.sourceSeed !== null && brain.sourceSeed !== undefined) {
    bits.push(`seed ${formatSeed(brain.sourceSeed)}`);
  }

  if (brain.sourceTick !== null && brain.sourceTick !== undefined) {
    bits.push(`tick ${formatNumber(brain.sourceTick)}`);
  }

  if (brain.sourceCreatureId) {
    bits.push(`creature #${formatNumber(brain.sourceCreatureId)}`);
  }

  if (brain.sourceGeneration !== null && brain.sourceGeneration !== undefined) {
    bits.push(`gen ${formatNumber(brain.sourceGeneration)}`);
  }

  return bits.join(", ");
}

function renderSpeciesCatalogOptions(selectedPath = "") {
  if (!speciesCatalogSelect) {
    return;
  }

  speciesCatalogSelect.innerHTML = "";
  const empty = document.createElement("option");
  empty.value = "";
  empty.textContent = speciesCatalog.length > 0 ? "Choose species profile" : "No species profiles";
  speciesCatalogSelect.append(empty);

  for (const species of speciesCatalog) {
    const option = document.createElement("option");
    option.value = species.path;
    option.textContent = `${species.name} (${species.path})`;
    option.title = [
      `${species.brainArchitectureKind}, hidden ${species.brainHiddenNodeCount}`,
      `speed ${formatDecimal(species.maxSpeed)}`,
      `sense ${formatDecimal(species.senseRadius)}`,
      species.sourceScenarioName ? `source ${species.sourceScenarioName}` : null
    ].filter(Boolean).join(" | ");
    speciesCatalogSelect.append(option);
  }

  speciesCatalogSelect.value = [...speciesCatalogSelect.options].some((option) => option.value === selectedPath)
    ? selectedPath
    : "";
  updateSpeciesCatalogButtons();
}

function selectedSpeciesCatalogEntry() {
  return speciesCatalog.find((candidate) => candidate.path === speciesCatalogSelect?.value) ?? null;
}

function renderSpeciesCatalogDetails() {
  if (!speciesCatalogDetails) {
    return;
  }

  const species = selectedSpeciesCatalogEntry();
  updateSpeciesCatalogButtons();
  if (!species) {
    speciesCatalogDetails.textContent = "Choose a species profile to inspect it.";
    return;
  }

  const defaultBrain = species.defaultBrainPath
    ? findBrainCatalogEntryByPath(species.defaultBrainPath)
    : null;
  const defaultBrainMissing = Boolean(species.defaultBrainPath && !defaultBrain);
  const defaultBrainStatus = species.defaultBrainPath
    ? defaultBrain
      ? brainCompatibilityStatus(defaultBrain)
      : "Default brain path is not present in the catalog."
    : "Uses embedded profile brain.";

  speciesCatalogDetails.innerHTML = `
    <div class="species-summary-grid">
      <div><span>Name</span><strong>${escapeHtml(species.name)}</strong></div>
      <div><span>Path</span><strong>${escapeHtml(species.path)}</strong></div>
      <div><span>Default brain</span><strong>${escapeHtml(species.defaultBrainPath || "embedded profile brain")}</strong></div>
      <div><span>Default brain status</span><strong class="${defaultBrain?.isCompatible === false || defaultBrainMissing ? "map-artifact-warning" : "map-artifact-ok"}">${escapeHtml(defaultBrainStatus)}</strong></div>
      <div><span>Brain</span><strong>${escapeHtml(species.brainArchitectureKind)}, hidden ${formatNumber(species.brainHiddenNodeCount)}, ${formatNumber(species.brainWeightCount)} weights</strong></div>
      <div><span>Body</span><strong>radius ${formatDecimal(species.bodyRadius)}, speed ${formatDecimal(species.maxSpeed)}, sense ${formatDecimal(species.senseRadius)}</strong></div>
      <div><span>Vision</span><strong>${formatDecimal(species.visionAngleDegrees)} deg</strong></div>
      <div><span>Energy</span><strong>basal ${formatDecimal(species.basalEnergyPerSecond)}/s, move ${formatDecimal(species.movementEnergyPerSecond)}/s, eat ${formatDecimal(species.eatCaloriesPerSecond)}/s</strong></div>
      <div><span>Reproduction</span><strong>threshold ${formatDecimal(species.reproductionEnergyThreshold)}, investment ${formatDecimal(species.offspringEnergyInvestment)}</strong></div>
      <div><span>Source</span><strong>${escapeHtml(formatSpeciesSource(species))}</strong></div>
    </div>
    ${species.notes ? `<div class="species-notes">${escapeHtml(species.notes)}</div>` : ""}
    ${defaultBrain ? renderBrainCompatibilityWarnings(defaultBrain) : ""}
  `;
}

function renderSpeciesRoster() {
  if (!speciesRosterDetails) {
    return;
  }

  if (!scenarioEditor) {
    speciesRosterDetails.textContent = "Choose a scenario to edit its starting roster.";
    return;
  }

  const roster = currentSpeciesRoster();
  if (roster.length === 0) {
    speciesRosterDetails.innerHTML = `<div class="species-roster-empty">No catalog species are in the starting roster.</div>`;
    return;
  }

  speciesRosterDetails.innerHTML = `
    <table class="species-roster-table">
      <thead>
        <tr>
          <th>Profile</th>
          <th>Label</th>
          <th>Brain for starters</th>
          <th>Count</th>
          <th>Region</th>
          <th>Energy</th>
          <th>Enabled</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        ${roster.map((seed, index) => renderSpeciesRosterRow(seed, index)).join("")}
      </tbody>
    </table>
  `;
}

function renderSpeciesRosterRow(seed, index) {
  const enabled = seed?.enabled !== false;
  const profile = findSpeciesCatalogEntryByPath(seed?.profilePath);
  const energyValue = seed?.energyOverride === null || seed?.energyOverride === undefined
    ? ""
    : String(seed.energyOverride);
  return `
    <tr class="${enabled ? "" : "is-disabled"}">
      <td>
        <strong>${escapeHtml(profile?.name || seed?.profilePath || "Missing species profile")}</strong>
        <span>${escapeHtml(seed?.profilePath || "")}</span>
      </td>
      <td>
        <input class="species-roster-text" type="text" placeholder="optional" value="${escapeHtml(seed?.label || "")}" data-species-roster-field="label" data-index="${index}">
      </td>
      <td>
        <select class="species-roster-select" data-species-roster-field="brain" data-index="${index}">
          ${brainChoiceOptionsHtml(speciesSeedBrainChoiceValue(seed), seed?.brainProfilePath || null, false)}
        </select>
      </td>
      <td>
        <input class="species-roster-number" type="number" min="1" step="1" value="${escapeHtml(String(seed?.count ?? 0))}" data-species-roster-field="count" data-index="${index}">
      </td>
      <td>
        <select class="species-roster-select" data-species-roster-field="spawnRegion" data-index="${index}">
          ${spawnRegionOptionsHtml(seed?.spawnRegion || "uniform")}
        </select>
      </td>
      <td>
        <input class="species-roster-number" type="number" min="1" step="1" placeholder="profile" value="${escapeHtml(energyValue)}" data-species-roster-field="energyOverride" data-index="${index}">
      </td>
      <td>
        <label class="species-roster-check">
          <input type="checkbox" ${enabled ? "checked" : ""} data-species-roster-field="enabled" data-index="${index}">
          <span>Enabled</span>
        </label>
      </td>
      <td class="species-roster-actions">
        <button class="danger" type="button" data-species-roster-action="remove" data-index="${index}">Remove</button>
      </td>
    </tr>
  `;
}

function spawnRegionOptionsHtml(selectedValue = "uniform") {
  const options = [
    ["uniform", "Uniform"],
    ["leftThird", "Left third"],
    ["middleThird", "Middle third"],
    ["rightThird", "Right third"],
    ["topThird", "Top third"],
    ["bottomThird", "Bottom third"],
    ["upperLeftQuadrant", "Upper-left quadrant"],
    ["upperRightQuadrant", "Upper-right quadrant"],
    ["lowerLeftQuadrant", "Lower-left quadrant"],
    ["lowerRightQuadrant", "Lower-right quadrant"]
  ];
  return options.map(([value, label]) =>
    `<option value="${escapeHtml(value)}"${value === selectedValue ? " selected" : ""}>${escapeHtml(label)}</option>`
  ).join("");
}

function currentSpeciesRoster() {
  return Array.isArray(scenarioEditor?.scenario?.speciesSeeds)
    ? scenarioEditor.scenario.speciesSeeds
    : [];
}

function findSpeciesCatalogEntryByPath(path) {
  return speciesCatalog.find((candidate) => candidate.path === path) ?? null;
}

function speciesSeedBrainChoiceValue(seed) {
  if (seed?.brainProfilePath) {
    return `catalog:${seed.brainProfilePath}`;
  }

  if (seed?.brainOverrideKind) {
    return `generated:${seed.brainOverrideKind}`;
  }

  return "profile";
}

function parseSpeciesBrainChoice(value) {
  const selected = value || "profile";
  if (selected.startsWith("catalog:")) {
    return {
      brainOverrideKind: null,
      brainProfilePath: selected.slice("catalog:".length)
    };
  }

  if (selected.startsWith("generated:")) {
    const kind = selected.slice("generated:".length);
    return {
      brainOverrideKind: kind === "scenario"
        ? scenarioEditor?.scenario?.initialBrainKind || "sectorForager"
        : kind,
      brainProfilePath: null
    };
  }

  return {
    brainOverrideKind: null,
    brainProfilePath: null
  };
}

function updateSpeciesRoster(action, index) {
  if (!scenarioEditor) {
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    formMessage.textContent = error.message;
    return;
  }

  const roster = Array.isArray(scenarioEditor.scenario.speciesSeeds)
    ? cloneJson(scenarioEditor.scenario.speciesSeeds)
    : [];
  if (index < 0 || index >= roster.length) {
    return;
  }

  if (action === "remove") {
    const [removed] = roster.splice(index, 1);
    scenarioEditor.scenario.speciesSeeds = roster;
    renderScenarioEditor();
    updateScenarioManagementButtons();
    formMessage.textContent = `Removed ${removed.profilePath || "species profile"} from the starting roster.`;
    return;
  }

  if (action === "toggle") {
    roster[index] = {
      ...roster[index],
      enabled: roster[index]?.enabled === false
    };
    scenarioEditor.scenario.speciesSeeds = roster;
    renderScenarioEditor();
    updateScenarioManagementButtons();
    formMessage.textContent = `${roster[index].enabled === false ? "Disabled" : "Enabled"} ${roster[index].profilePath || "species profile"} in the starting roster.`;
  }
}

function updateSpeciesRosterField(control) {
  if (!scenarioEditor || !control) {
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    formMessage.textContent = error.message;
    return;
  }

  const index = Number(control.dataset.index);
  const field = control.dataset.speciesRosterField;
  const roster = Array.isArray(scenarioEditor.scenario.speciesSeeds)
    ? cloneJson(scenarioEditor.scenario.speciesSeeds)
    : [];
  if (index < 0 || index >= roster.length || !field) {
    return;
  }

  const next = { ...roster[index] };
  if (field === "count") {
    const count = Math.round(Number(control.value));
    if (!Number.isFinite(count) || count <= 0) {
      formMessage.textContent = "Roster count must be a positive whole number.";
      renderSpeciesRoster();
      return;
    }

    next.count = count;
  } else if (field === "spawnRegion") {
    next.spawnRegion = control.value || "uniform";
  } else if (field === "energyOverride") {
    const raw = control.value.trim();
    const energyOverride = raw === "" ? null : Number(raw);
    if (raw !== "" && (!Number.isFinite(energyOverride) || energyOverride <= 0)) {
      formMessage.textContent = "Roster energy override must be blank or a positive number.";
      renderSpeciesRoster();
      return;
    }

    next.energyOverride = energyOverride;
  } else if (field === "label") {
    const label = control.value.trim();
    next.label = label === "" ? null : label;
  } else if (field === "brain") {
    const brainChoice = parseSpeciesBrainChoice(control.value);
    const selectedBrainProfile = brainChoice.brainProfilePath
      ? findBrainCatalogEntryByPath(brainChoice.brainProfilePath)
      : null;
    if (brainChoice.brainProfilePath && selectedBrainProfile?.isCompatible === false) {
      formMessage.textContent = `${selectedBrainProfile.name} is not compatible with the current runtime: ${brainCompatibilityStatus(selectedBrainProfile)}`;
      renderSpeciesRoster();
      return;
    }

    next.brainOverrideKind = brainChoice.brainOverrideKind;
    next.brainProfilePath = brainChoice.brainProfilePath;
  } else if (field === "enabled") {
    next.enabled = Boolean(control.checked);
  }

  roster[index] = next;
  scenarioEditor.scenario.speciesSeeds = roster;
  renderSpeciesRoster();
  updateScenarioEditorStatus();
  updateScenarioManagementButtons();
  formMessage.textContent = `Updated ${next.profilePath || "species profile"} in the starting roster.`;
}

function updateSpeciesCatalogButtons() {
  const species = selectedSpeciesCatalogEntry();
  if (addSpeciesToScenarioButton) {
    addSpeciesToScenarioButton.disabled = !species || !scenarioEditor;
  }

  if (deleteSpeciesCatalogButton) {
    deleteSpeciesCatalogButton.disabled = !species?.canDelete;
  }
}

function formatSpeciesSource(species) {
  const bits = [];
  if (species.sourceScenarioName) {
    bits.push(species.sourceScenarioName);
  }

  if (species.sourceSeed !== null && species.sourceSeed !== undefined) {
    bits.push(`seed ${formatSeed(species.sourceSeed)}`);
  }

  if (species.sourceTick !== null && species.sourceTick !== undefined) {
    bits.push(`tick ${formatNumber(species.sourceTick)}`);
  }

  if (species.sourceCreatureId) {
    bits.push(`creature #${formatNumber(species.sourceCreatureId)}`);
  }

  if (species.sourceGeneration !== null && species.sourceGeneration !== undefined) {
    bits.push(`gen ${formatNumber(species.sourceGeneration)}`);
  }

  return bits.join(", ");
}

function renderMapArtifactOptions(selectedPath = "") {
  if (!mapArtifactSelect) {
    return;
  }

  mapArtifactSelect.innerHTML = "";
  const empty = document.createElement("option");
  empty.value = "";
  empty.textContent = mapArtifacts.length > 0 ? "Choose map" : "No saved maps";
  mapArtifactSelect.append(empty);

  for (const map of mapArtifacts) {
    const option = document.createElement("option");
    option.value = map.path;
    const wallCount = Number(map.obstacleBlockedCellCount || 0);
    option.textContent = `${map.name} - ${formatDecimal(map.worldWidth)} x ${formatDecimal(map.worldHeight)} (${map.path})`;
    option.title = [
      `${formatDecimal(map.worldWidth)} x ${formatDecimal(map.worldHeight)}`,
      `${formatNumber(map.biomeCellCountX)} x ${formatNumber(map.biomeCellCountY)} biome cells`,
      wallCount > 0 ? `${formatNumber(wallCount)} walls` : "no walls",
      map.sourceSeed === null || map.sourceSeed === undefined ? null : `seed ${formatSeed(map.sourceSeed)}`
    ].filter(Boolean).join(" | ");
    mapArtifactSelect.append(option);
  }

  if ([...mapArtifactSelect.options].some((option) => option.value === selectedPath)) {
    mapArtifactSelect.value = selectedPath;
  } else {
    mapArtifactSelect.value = "";
  }
}

function selectedMapArtifact() {
  return mapArtifacts.find((candidate) => candidate.path === mapArtifactSelect?.value) ?? null;
}

function renderMapArtifactDetails() {
  if (!mapArtifactDetails) {
    return;
  }

  const map = selectedMapArtifact();
  if (!map) {
    mapArtifactDetails.hidden = true;
    mapArtifactDetails.innerHTML = "";
    return;
  }

  const scenario = scenarioEditor?.scenario ?? null;
  const compatibility = scenario ? mapArtifactCompatibility(map, scenario) : [];
  const biomeSummary = Array.isArray(map.biomes)
    ? map.biomes
        .filter((biome) => Number(biome.cellCount || 0) > 0)
        .map((biome) => `
          <span class="map-artifact-biome">
            <span class="biome-preview-swatch" style="background:${escapeHtml(biome.color)}"></span>
            ${escapeHtml(biome.name)} ${escapeHtml(formatPercent(biome.areaShare))}
          </span>
        `).join("")
    : "";
  const wallShare = Number(map.obstacleCellCountX || 0) * Number(map.obstacleCellCountY || 0) > 0
    ? Number(map.obstacleBlockedCellCount || 0) / (Number(map.obstacleCellCountX || 0) * Number(map.obstacleCellCountY || 0))
    : 0;
  const compatibilityHtml = compatibility.length === 0
    ? `<div class="map-artifact-ok">Compatible with the current scenario dimensions.</div>`
    : `
      <div class="map-artifact-warning">Apply Map will update scenario dimensions/settings:</div>
      <ul>${compatibility.map((item) => `<li>${escapeHtml(item)}</li>`).join("")}</ul>
    `;

  mapArtifactDetails.hidden = false;
  mapArtifactDetails.innerHTML = `
    <div class="map-artifact-summary">
      <strong>${escapeHtml(map.name)}</strong>
      <span>${escapeHtml(map.path)}</span>
    </div>
    <div class="map-artifact-facts">
      <span>${escapeHtml(formatDecimal(map.worldWidth))} x ${escapeHtml(formatDecimal(map.worldHeight))}</span>
      <span>${escapeHtml(formatNumber(map.biomeCellCountX))} x ${escapeHtml(formatNumber(map.biomeCellCountY))} biome cells @ ${escapeHtml(formatDecimal(map.biomeCellSize))}u</span>
      <span>${escapeHtml(formatNumber(map.obstacleBlockedCellCount))} walls (${escapeHtml(formatPercent(wallShare))})</span>
      <span>${map.sourceSeed === null || map.sourceSeed === undefined ? "seed n/a" : `seed ${escapeHtml(formatSeed(map.sourceSeed))}`}</span>
    </div>
    ${biomeSummary ? `<div class="map-artifact-biomes">${biomeSummary}</div>` : ""}
    <div class="map-artifact-compatibility">${compatibilityHtml}</div>
  `;
}

function mapArtifactCompatibility(map, scenario) {
  const differences = [];
  addNumericDifference(differences, "World width", scenario.worldWidth, map.worldWidth);
  addNumericDifference(differences, "World height", scenario.worldHeight, map.worldHeight);
  addNumericDifference(differences, "Biome cell size", scenario.biomeCellSize, map.biomeCellSize);
  addNumericDifference(differences, "Resource void border", scenario.resourceVoidBorderWidth, map.resourceVoidBorderWidth);
  addNumericDifference(differences, "Obstacle cell size", scenario.obstacleCellSize, map.obstacleCellSize);

  if (scenario.worldMapPath && scenario.worldMapPath !== map.path) {
    differences.push(`World map path: ${scenario.worldMapPath} -> ${map.path}`);
  }

  if (scenario.biomeMapKind && scenario.biomeMapKind !== "manual") {
    differences.push(`Biome map kind: ${scenario.biomeMapKind} -> manual`);
  }

  const hasWalls = Number(map.obstacleBlockedCellCount || 0) > 0;
  const targetObstacleKind = hasWalls ? "manual" : "none";
  if ((scenario.obstacleMapKind || "none") !== targetObstacleKind) {
    differences.push(`Obstacle map kind: ${scenario.obstacleMapKind || "none"} -> ${targetObstacleKind}`);
  }

  return differences;
}

function addNumericDifference(differences, label, currentValue, mapValue) {
  const current = Number(currentValue);
  const next = Number(mapValue);
  if (!Number.isFinite(current) || !Number.isFinite(next)) {
    return;
  }

  if (Math.abs(current - next) > 0.0001) {
    differences.push(`${label}: ${formatDecimal(current)} -> ${formatDecimal(next)}`);
  }
}

async function loadScenarioRecipes() {
  const response = await fetch("/api/scenario-recipes");
  scenarioRecipes = response.ok ? await response.json() : [];
  renderRecipePicker();
  renderRecipeStack();
}

function scenarioOptionLabel(scenario) {
  const suffix = scenario.isUserCreated ? " [saved]" : "";
  return `${scenario.name}${suffix} (${scenario.path})`;
}

function selectedScenarioOption() {
  const selectedPath = scenarioSelect.value;
  const fromList = scenarioOptions.find((scenario) => scenario.path === selectedPath);
  if (fromList) {
    return fromList;
  }

  const selectedOption = scenarioSelect.selectedOptions[0];
  return selectedOption
    ? {
        name: selectedOption.textContent,
        path: selectedPath,
        isUserCreated: selectedOption.dataset.isUserCreated === "true",
        canDelete: selectedOption.dataset.canDelete === "true"
      }
    : null;
}

function updateScenarioManagementButtons() {
  if (saveScenarioButton) {
    saveScenarioButton.disabled = !scenarioEditor;
  }

  if (saveRecipeButton) {
    saveRecipeButton.disabled = !scenarioEditor || changedScenarioFields().length === 0;
  }

  if (saveNewRecipeButton) {
    saveNewRecipeButton.disabled = !scenarioEditor || buildNewRecipeChanges().length === 0;
  }

  if (applyRecipeButton) {
    applyRecipeButton.disabled = !scenarioEditor || !selectedRecipe();
  }

  if (archiveRecipeButton) {
    archiveRecipeButton.disabled = !selectedRecipe();
  }

  if (deleteRecipeButton) {
    deleteRecipeButton.disabled = !selectedRecipe();
  }

  if (reviewLaunchDiffButton) {
    reviewLaunchDiffButton.disabled = !scenarioEditor;
  }

  if (deleteScenarioButton) {
    const option = selectedScenarioOption();
    deleteScenarioButton.disabled = !option?.canDelete;
    deleteScenarioButton.title = option?.canDelete
      ? "Archive this launcher-saved scenario."
      : "Built-in scenarios and manually added scenario files are protected.";
  }
}

async function loadScenarioEditor() {
  scenarioEditor = null;
  scenarioEditorBaseline = null;
  activeScenarioGroup = null;
  appliedRecipes = [];
  recipeBaseScenario = null;
  recipeDiffCheckpoint = null;
  closeScenarioDiffPanel();
  scenarioTabs.innerHTML = "";
  scenarioFields.innerHTML = "";
  renderRecipeStack();
  scenarioOptionsStatus.textContent = "Loading options...";
  clearBiomePreview("Loading scenario preview...");
  updateScenarioResetButtons();
  updateScenarioManagementButtons();
  updateSpeciesCatalogButtons();

  if (!scenarioSelect.value) {
    scenarioOptionsStatus.textContent = "No scenario selected.";
    clearBiomePreview("Choose a scenario to preview its biome layout.");
    updateScenarioManagementButtons();
    updateSpeciesCatalogButtons();
    return;
  }

  const response = await fetch(`/api/scenario-editor?path=${encodeURIComponent(scenarioSelect.value)}`);
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Scenario options unavailable." }));
    scenarioOptionsStatus.textContent = problem.error || "Scenario options unavailable.";
    clearBiomePreview(problem.error || "Biome preview unavailable.");
    updateScenarioManagementButtons();
    updateSpeciesCatalogButtons();
    return;
  }

  resetScenarioOptionFilters();
  loadScenarioEditorDefinition(await response.json());
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

  const activeFields = filteredScenarioFields().filter((field) => field.group === activeScenarioGroup);
  for (const field of activeFields) {
    scenarioFields.append(createScenarioField(field));
  }

  if (activeFields.length === 0) {
    const empty = document.createElement("div");
    empty.className = "scenario-options-empty";
    empty.textContent = "No scenario options match.";
    scenarioFields.append(empty);
  }

  updateScenarioEditorStatus();
  renderSpeciesRoster();
}

function renderRecipePicker() {
  if (!recipeOptions) {
    return;
  }

  const query = recipePicker.value.trim().toLowerCase();
  const visibleRecipes = scenarioRecipes.filter((recipe) => recipeMatchesSearch(recipe, query));
  recipeOptions.innerHTML = "";
  for (const recipe of visibleRecipes) {
    const option = document.createElement("option");
    option.value = recipeOptionLabel(recipe);
    option.label = recipePickerDetail(recipe);
    recipeOptions.append(option);
  }

  updateScenarioManagementButtons();
}

function selectedRecipe() {
  const value = recipePicker?.value.trim() || "";
  if (!value) {
    return null;
  }

  return scenarioRecipes.find((recipe) =>
    recipeOptionLabel(recipe) === value
    || recipe.name === value
    || recipe.path === value) ?? null;
}

function recipeMatchesSearch(recipe, query) {
  if (!query) {
    return true;
  }

  return [
    recipe.name,
    recipe.description,
    recipe.path,
    ...(recipe.tags || []),
    ...recipeFieldNames(recipe).map((field) => scenarioFieldLabel(field))
  ].some((value) => String(value ?? "").toLowerCase().includes(query));
}

function renderRecipeStack() {
  if (!recipeStack) {
    return;
  }

  if (!scenarioEditor) {
    recipeStack.innerHTML = "";
    return;
  }

  if (appliedRecipes.length === 0) {
    recipeStack.innerHTML = `<div class="recipe-stack-empty">No recipes applied.</div>`;
    return;
  }

  recipeStack.innerHTML = appliedRecipes.map((recipe, index) => {
    const fields = recipeFieldNames(recipe);
    const overrideCount = countPriorRecipeOverrides(index);
    const title = fields.map((field) => scenarioFieldLabel(field)).join(", ");
    return `
      <div class="recipe-stack-item">
        <div>
          <strong>${escapeHtml(recipe.name)}</strong>
          <span>${fields.length} field${fields.length === 1 ? "" : "s"}${overrideCount > 0 ? `, overrides ${overrideCount}` : ""}</span>
          ${title ? `<div class="run-sub">${escapeHtml(title)}</div>` : ""}
        </div>
        <div class="recipe-stack-actions">
          <button class="secondary" type="button" data-recipe-action="up" data-index="${index}" ${index === 0 ? "disabled" : ""}>Up</button>
          <button class="secondary" type="button" data-recipe-action="down" data-index="${index}" ${index === appliedRecipes.length - 1 ? "disabled" : ""}>Down</button>
          <button class="secondary" type="button" data-recipe-action="remove" data-index="${index}">Remove</button>
        </div>
      </div>
    `;
  }).join("");
}

function recipeOptionLabel(recipe) {
  const tags = Array.isArray(recipe.tags) && recipe.tags.length > 0
    ? ` [${recipe.tags.join(", ")}]`
    : "";
  return `${recipe.name}${tags} - ${recipe.path} (${recipeFieldNames(recipe).length} fields)`;
}

function recipePickerDetail(recipe) {
  return recipeFieldNames(recipe).map((field) => scenarioFieldLabel(field)).join(", ");
}

function recipeFieldNames(recipe) {
  return Object.keys(recipe?.changes || {});
}

function scenarioFieldLabel(jsonName) {
  const field = scenarioEditor?.fields.find((candidate) => candidate.jsonName === jsonName);
  return field?.label || jsonName;
}

function countPriorRecipeOverrides(index) {
  const currentFields = new Set(recipeFieldNames(appliedRecipes[index]));
  let count = 0;
  for (let priorIndex = 0; priorIndex < index; priorIndex++) {
    for (const field of recipeFieldNames(appliedRecipes[priorIndex])) {
      if (currentFields.has(field)) {
        count++;
      }
    }
  }

  return count;
}

function applySelectedRecipe() {
  if (!scenarioEditor) {
    return;
  }

  const recipe = selectedRecipe();
  if (!recipe) {
    formMessage.textContent = "Recipe is not available.";
    return;
  }

  try {
    let manualOverrides = {};
    if (appliedRecipes.length === 0) {
      storeVisibleScenarioValues();
      recipeBaseScenario = cloneJson(scenarioEditor.scenario);
    } else {
      manualOverrides = captureManualScenarioOverrides();
    }

    appliedRecipes.push(recipe);
    recomposeScenarioFromRecipes(manualOverrides);
    recipeDiffCheckpoint = cloneJson(scenarioEditor.scenario);
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
    return;
  }

  recipePicker.value = "";
  renderRecipePicker();
  updateScenarioManagementButtons();
  formMessage.textContent = `Applied recipe ${recipe.name}.`;
}

function updateRecipeStack(action, index) {
  if (!scenarioEditor || index < 0 || index >= appliedRecipes.length) {
    return;
  }

  try {
    const manualOverrides = captureManualScenarioOverrides();
    if (action === "remove") {
      appliedRecipes.splice(index, 1);
    } else if (action === "up" && index > 0) {
      [appliedRecipes[index - 1], appliedRecipes[index]] = [appliedRecipes[index], appliedRecipes[index - 1]];
    } else if (action === "down" && index < appliedRecipes.length - 1) {
      [appliedRecipes[index + 1], appliedRecipes[index]] = [appliedRecipes[index], appliedRecipes[index + 1]];
    }

    recomposeScenarioFromRecipes(manualOverrides);
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
  }
}

function captureManualScenarioOverrides() {
  storeVisibleScenarioValues();
  const recipeScenario = composeScenarioFromRecipes(appliedRecipes);
  return diffScenarioFields(scenarioEditor.scenario, recipeScenario);
}

function recomposeScenarioFromRecipes(manualOverrides = {}) {
  const scenario = composeScenarioFromRecipes(appliedRecipes);
  for (const [jsonName, value] of Object.entries(manualOverrides)) {
    scenario[jsonName] = cloneJson(value);
  }

  scenarioEditor.scenario = scenario;
  renderScenarioEditor();
  renderRecipeStack();
  scheduleBiomePreview();
}

function composeScenarioFromRecipes(recipes) {
  const scenario = cloneJson(recipeBaseScenario || scenarioEditorBaseline);
  for (const recipe of recipes) {
    applyRecipeChanges(scenario, recipe.changes);
  }

  return scenario;
}

function applyRecipeChanges(scenario, changes) {
  for (const [jsonName, value] of Object.entries(changes || {})) {
    if (scenarioEditor.fields.some((field) => field.jsonName === jsonName)) {
      scenario[jsonName] = cloneJson(value);
    }
  }
}

function diffScenarioFields(leftScenario, rightScenario) {
  const changes = {};
  for (const field of scenarioEditor.fields) {
    if (stableStringify(leftScenario?.[field.jsonName]) !== stableStringify(rightScenario?.[field.jsonName])) {
      changes[field.jsonName] = cloneJson(leftScenario?.[field.jsonName]);
    }
  }

  return changes;
}

function buildNewRecipeChanges() {
  if (!scenarioEditor || !recipeDiffCheckpoint) {
    return [];
  }

  return buildScenarioDiffRows(recipeDiffCheckpoint, scenarioEditor.scenario);
}

function buildScenarioDiffRows(leftScenario, rightScenario) {
  if (!scenarioEditor) {
    return [];
  }

  return scenarioEditor.fields
    .filter((field) => stableStringify(leftScenario?.[field.jsonName]) !== stableStringify(rightScenario?.[field.jsonName]))
    .map((field) => ({
      jsonName: field.jsonName,
      label: field.label,
      group: field.group,
      before: cloneJson(leftScenario?.[field.jsonName]),
      after: cloneJson(rightScenario?.[field.jsonName])
    }));
}

function showScenarioDiffPanel(title, rows, summary, options = {}) {
  scenarioDiffTitle.textContent = title;
  scenarioDiffSummary.textContent = summary;
  confirmSaveRecipeButton.hidden = !options.confirmSave;
  confirmSaveRecipeButton.textContent = options.confirmLabel || "Save Recipe";
  scenarioDiffBody.innerHTML = rows.length === 0
    ? `<tr><td class="empty" colspan="4">No scenario option changes.</td></tr>`
    : rows.map((row) => `
      <tr>
        <td>
          <div class="artifact-label">${escapeHtml(row.label)}</div>
          <div class="run-sub">${escapeHtml(row.jsonName)}</div>
        </td>
        <td>${escapeHtml(row.group)}</td>
        <td><pre>${escapeHtml(formatScenarioValue(row.before))}</pre></td>
        <td><pre>${escapeHtml(formatScenarioValue(row.after))}</pre></td>
      </tr>
    `).join("");
  scenarioDiffPanel.hidden = false;
}

function closeScenarioDiffPanel() {
  scenarioDiffPanel.hidden = true;
  pendingRecipeSave = null;
  confirmSaveRecipeButton.hidden = true;
}

function formatScenarioValue(value) {
  if (value === undefined) {
    return "";
  }

  if (value === null || typeof value !== "object") {
    return String(value);
  }

  return JSON.stringify(value, null, 2);
}

function reviewLaunchDiff() {
  if (!scenarioEditor) {
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
    return;
  }

  const rows = buildScenarioDiffRows(scenarioEditorBaseline, scenarioEditor.scenario);
  showScenarioDiffPanel(
    "Launch Diff",
    rows,
    rows.length === 0
      ? "No scenario option changes from the loaded base scenario."
      : `${rows.length} scenario option${rows.length === 1 ? "" : "s"} will differ from the loaded base scenario.`);
}

function saveCurrentDiffAsRecipe() {
  prepareRecipeSave("full");
}

function saveNewDiffsAsRecipe() {
  prepareRecipeSave("new");
}

function prepareRecipeSave(mode) {
  if (!scenarioEditor) {
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
    return;
  }

  const baseScenario = mode === "new"
    ? recipeDiffCheckpoint || scenarioEditorBaseline
    : scenarioEditorBaseline;
  const changes = diffScenarioFields(scenarioEditor.scenario, baseScenario);
  const rows = buildScenarioDiffRows(baseScenario, scenarioEditor.scenario);
  const changeCount = Object.keys(changes).length;
  if (changeCount === 0) {
    formMessage.textContent = mode === "new"
      ? "No new scenario option changes since the last recipe save or apply."
      : "Change at least one scenario option before saving a recipe.";
    return;
  }

  const name = prompt(mode === "new" ? "Save new diffs as recipe" : "Save recipe as", "New Recipe");
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    formMessage.textContent = "Recipe name is required.";
    return;
  }

  const tagsText = prompt("Recipe tags, comma-separated", "");
  if (tagsText === null) {
    return;
  }

  pendingRecipeSave = {
    name: trimmedName,
    description: "",
    tags: tagsText.split(",").map((tag) => tag.trim()).filter(Boolean),
    changes,
    changeCount,
    mode
  };
  showScenarioDiffPanel(
    `Save Recipe: ${trimmedName}`,
    rows,
    mode === "new"
      ? `${changeCount} new field${changeCount === 1 ? "" : "s"} will be saved since the last recipe save/apply point.`
      : `${changeCount} field${changeCount === 1 ? "" : "s"} will be saved from the full base-scenario diff.`,
    { confirmSave: true, confirmLabel: "Save Recipe" });
}

async function confirmPendingRecipeSave() {
  if (!pendingRecipeSave) {
    return;
  }

  saveRecipeButton.disabled = true;
  formMessage.textContent = "Saving recipe...";
  const response = await fetch("/api/scenario-recipes", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(pendingRecipeSave)
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Recipe save failed." }));
    formMessage.textContent = problem.error || "Recipe save failed.";
    updateScenarioManagementButtons();
    return;
  }

  const result = await response.json();
  const changeCount = pendingRecipeSave.changeCount;
  pendingRecipeSave = null;
  scenarioDiffPanel.hidden = true;
  recipeDiffCheckpoint = cloneJson(scenarioEditor.scenario);
  await loadScenarioRecipes();
  recipePicker.value = recipeOptionLabel(result.recipe);
  renderRecipePicker();
  formMessage.textContent = `Saved recipe ${result.recipe.name} with ${changeCount} field${changeCount === 1 ? "" : "s"}.`;
  updateScenarioManagementButtons();
}

async function archiveSelectedRecipe() {
  const recipe = selectedRecipe();
  if (!recipe) {
    formMessage.textContent = "Choose a recipe to archive.";
    return;
  }

  if (!confirm(`Archive recipe "${recipe.name}"?`)) {
    return;
  }

  archiveRecipeButton.disabled = true;
  formMessage.textContent = "Archiving recipe...";
  const response = await fetch(`/api/scenario-recipes?path=${encodeURIComponent(recipe.path)}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Recipe archive failed." }));
    formMessage.textContent = problem.error || "Recipe archive failed.";
    updateScenarioManagementButtons();
    return;
  }

  const result = await response.json();
  scenarioRecipes = scenarioRecipes.filter((candidate) => candidate.path !== recipe.path);
  recipePicker.value = "";
  renderRecipePicker();
  renderRecipeStack();
  formMessage.textContent = `Archived recipe to ${result.archivedPath}.`;
  updateScenarioManagementButtons();
}

async function deleteSelectedRecipe() {
  const recipe = selectedRecipe();
  if (!recipe) {
    formMessage.textContent = "Choose a recipe to delete.";
    return;
  }

  if (!confirm(`Permanently delete recipe "${recipe.name}"? This cannot be undone.`)) {
    return;
  }

  deleteRecipeButton.disabled = true;
  formMessage.textContent = "Deleting recipe...";
  const response = await fetch(`/api/scenario-recipes/permanent?path=${encodeURIComponent(recipe.path)}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Recipe delete failed." }));
    formMessage.textContent = problem.error || "Recipe delete failed.";
    updateScenarioManagementButtons();
    return;
  }

  scenarioRecipes = scenarioRecipes.filter((candidate) => candidate.path !== recipe.path);
  recipePicker.value = "";
  renderRecipePicker();
  renderRecipeStack();
  formMessage.textContent = `Deleted recipe ${recipe.name}.`;
  updateScenarioManagementButtons();
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
    "Performance",
    "Species",
    "Advanced"
  ];
  const groups = [...new Set(filteredScenarioFields().map((field) => field.group))];
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

function filteredScenarioFields() {
  if (!scenarioEditor) {
    return [];
  }

  return scenarioEditor.fields.filter((field) =>
    scenarioFieldMatchesScope(field) && scenarioFieldMatchesSearch(field));
}

function scenarioFieldMatchesScope(field) {
  return showAdvancedScenarioOptions() || !field.advanced;
}

function scenarioFieldMatchesSearch(field) {
  const query = scenarioOptionSearch.value.trim().toLowerCase();
  if (query.length === 0) {
    return true;
  }

  return [
    field.name,
    field.jsonName,
    field.label,
    field.group,
    field.units,
    field.description
  ].some((value) => String(value ?? "").toLowerCase().includes(query));
}

function showAdvancedScenarioOptions() {
  return scenarioScopeInputs.some((input) => input.checked && input.value === "all");
}

function loadScenarioEditorDefinition(editor) {
  scenarioEditor = editor;
  scenarioEditorBaseline = cloneJson(editor.scenario);
  appliedRecipes = [];
  recipeBaseScenario = cloneJson(editor.scenario);
  recipeDiffCheckpoint = cloneJson(editor.scenario);
  activeScenarioGroup = scenarioEditorGroups()[0] || null;
  biomePaintDirty = false;
  setBiomePaintEnabled(false);
  renderMapArtifactOptions(editor.scenario?.worldMapPath ?? mapArtifactSelect?.value ?? "");
  renderMapArtifactDetails();
  renderScenarioEditor();
  renderRecipeStack();
  updateScenarioManagementButtons();
  updateSpeciesCatalogButtons();
  renderSpeciesRoster();
  scheduleBiomePreview();
}

function scheduleBiomePreview(delayMs = 250) {
  if (!biomePreviewCanvas) {
    return;
  }

  window.clearTimeout(biomePreviewTimer);
  biomePreviewTimer = window.setTimeout(updateBiomePreview, delayMs);
}

function readBiomePreviewCollapsed() {
  try {
    return window.localStorage.getItem("lineage.biomePreviewCollapsed") === "true";
  } catch {
    return false;
  }
}

function setBiomePreviewCollapsed(collapsed, persist = true) {
  biomePreviewCollapsed = collapsed;
  if (biomePreviewContent) {
    biomePreviewContent.hidden = collapsed;
  }

  if (biomePreview) {
    biomePreview.classList.toggle("is-collapsed", collapsed);
  }

  if (toggleBiomePreviewButton) {
    toggleBiomePreviewButton.textContent = collapsed ? "Show Map" : "Hide Map";
    toggleBiomePreviewButton.setAttribute("aria-expanded", String(!collapsed));
  }

  if (persist) {
    try {
      window.localStorage.setItem("lineage.biomePreviewCollapsed", String(collapsed));
    } catch {
      // Preference storage is best-effort; the toggle should still work.
    }
  }

  if (!collapsed && currentBiomePreview) {
    window.requestAnimationFrame(() => drawBiomePreview(currentBiomePreview));
  }
}

function setBiomePaintEnabled(enabled) {
  biomePaintEnabled = Boolean(
    enabled
    && currentBiomePreview
    && (currentBiomePreview.enabled || currentBiomePreview.obstacleCells?.length));
  if (biomePaintEnabled && biomePreviewCollapsed) {
    setBiomePreviewCollapsed(false);
  }

  updateBiomePaintControls();
  if (biomePreviewStatus && currentBiomePreview) {
    const layer = paintLayerSelect?.value === "obstacle" ? "wall" : "biome";
    biomePreviewStatus.textContent = biomePaintEnabled
      ? `Painting ${layer} cells. Drag on the map, then save a reusable map.`
      : biomePaintDirty
        ? "Map edits are unsaved."
        : "Preview reflects the scenario options and launch seed override above.";
  }
}

function updateBiomePaintControls() {
  const canEditBiomes = Boolean(currentBiomePreview?.enabled && currentBiomePreview?.cells?.length);
  const canEditObstacles = Boolean(currentBiomePreview?.obstacleCells?.length);
  const canEdit = canEditBiomes || canEditObstacles;
  const layer = paintLayerSelect?.value === "obstacle" ? "obstacle" : "biome";
  const selectedMap = selectedMapArtifact();
  if (!canEdit) {
    biomePaintEnabled = false;
    biomePaintDirty = false;
  }

  if (paintLayerSelect) {
    paintLayerSelect.disabled = !canEdit;
  }

  if (biomeBrushSelect) {
    biomeBrushSelect.disabled = !canEditBiomes || layer !== "biome";
  }

  if (obstacleBrushSelect) {
    obstacleBrushSelect.disabled = !canEditObstacles || layer !== "obstacle";
  }

  if (paintBiomeMapButton) {
    paintBiomeMapButton.disabled = !canEdit;
    paintBiomeMapButton.textContent = biomePaintEnabled ? "Stop Painting" : "Paint Map";
  }

  if (mapArtifactSelect) {
    mapArtifactSelect.disabled = mapArtifacts.length === 0;
  }

  if (applyMapArtifactButton) {
    applyMapArtifactButton.disabled = !selectedMap;
  }

  if (saveMapArtifactButton) {
    saveMapArtifactButton.disabled = !currentBiomePreview;
    saveMapArtifactButton.textContent = biomePaintDirty ? "Save Reusable Map *" : "Save Reusable Map";
  }

  if (renameMapArtifactButton) {
    renameMapArtifactButton.disabled = !selectedMap?.canDelete;
    renameMapArtifactButton.title = selectedMap?.canDelete
      ? "Rename this reusable map artifact."
      : "Only launcher-managed user maps can be renamed.";
  }

  if (duplicateMapArtifactButton) {
    duplicateMapArtifactButton.disabled = !selectedMap?.canDelete;
    duplicateMapArtifactButton.title = selectedMap?.canDelete
      ? "Copy this reusable map artifact."
      : "Only launcher-managed user maps can be duplicated.";
  }

  if (deleteMapArtifactButton) {
    deleteMapArtifactButton.disabled = !selectedMap?.canDelete;
    deleteMapArtifactButton.title = selectedMap?.canDelete
      ? "Archive this reusable map artifact."
      : "Only launcher-managed user maps can be deleted.";
  }

  renderMapArtifactDetails();

  if (biomePreview) {
    biomePreview.classList.toggle("is-painting", biomePaintEnabled);
  }
}

async function updateBiomePreview() {
  if (!scenarioEditor || !biomePreviewCanvas) {
    clearBiomePreview("Choose a scenario to preview its biome layout.");
    return;
  }

  let scenario;
  try {
    scenario = collectScenarioOptions();
  } catch (error) {
    clearBiomePreview(error.message);
    return;
  }

  const requestId = ++biomePreviewRequestId;
  biomePreviewStatus.textContent = "Rendering biome preview...";
  refreshBiomePreviewButton.disabled = true;
  const seedOverride = valueOrNull("#seed");
  try {
    const response = await fetch("/api/scenario-preview/biome-map", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ scenario, seed: seedOverride, scenarioPath: scenarioSelect.value })
    });

    if (requestId !== biomePreviewRequestId) {
      return;
    }

    if (!response.ok) {
      const problem = await response.json().catch(() => ({ error: "Biome preview unavailable." }));
      clearBiomePreview(problem.error || "Biome preview unavailable.");
      return;
    }

    currentBiomePreview = normalizeBiomePreview(await response.json());
    biomePaintDirty = false;
    setBiomePaintEnabled(false);
    renderBiomePreview(currentBiomePreview, seedOverride !== null);
  } catch (error) {
    if (requestId === biomePreviewRequestId) {
      clearBiomePreview(error.message || "Biome preview unavailable.");
    }
  } finally {
    if (requestId === biomePreviewRequestId) {
      refreshBiomePreviewButton.disabled = false;
    }
  }
}

function clearBiomePreview(message) {
  currentBiomePreview = null;
  biomePaintDirty = false;
  biomePaintEnabled = false;
  if (biomePreviewMeta) {
    biomePreviewMeta.textContent = "";
  }

  if (biomePreviewLegend) {
    biomePreviewLegend.innerHTML = "";
  }

  if (biomePreviewStatus) {
    biomePreviewStatus.textContent = message || "";
  }

  if (biomePreviewCanvas) {
    const context = biomePreviewCanvas.getContext("2d");
    context.clearRect(0, 0, biomePreviewCanvas.width, biomePreviewCanvas.height);
  }

  updateBiomePaintControls();
}

function renderBiomePreview(preview, seedIsOverride) {
  if (!preview || !biomePreviewCanvas) {
    return;
  }

  normalizeBiomePreview(preview);
  const seedNote = seedIsOverride ? "launch override" : "scenario";
  biomePreviewMeta.textContent = [
    `${preview.mapKind}${preview.enabled ? "" : " (disabled)"}`,
    `${formatDecimal(preview.worldWidth)} x ${formatDecimal(preview.worldHeight)}`,
    `${formatNumber(preview.cellCountX)} x ${formatNumber(preview.cellCountY)} cells`,
    `seed ${formatSeed(preview.seed)} (${seedNote})`
  ].join(" | ");
  renderBiomeBrushOptions(preview);
  drawBiomePreview(preview);
  renderBiomePreviewLegend(preview);
  biomePreviewStatus.textContent = preview.enabled
    ? "Preview reflects the scenario options and launch seed override above."
    : "Biomes are disabled; the map is shown as uniform grassland.";
  updateBiomePaintControls();
}

function normalizeBiomePreview(preview) {
  if (!preview) {
    return preview;
  }

  const fallbackScenario = scenarioEditor?.scenario || {};
  preview.worldWidth = finitePositiveNumber(preview.worldWidth, fallbackScenario.worldWidth || 1);
  preview.worldHeight = finitePositiveNumber(preview.worldHeight, fallbackScenario.worldHeight || 1);
  preview.obstacleCellSize = finitePositiveNumber(
    preview.obstacleCellSize,
    fallbackScenario.obstacleCellSize || 128);
  preview.obstacleCellCountX = finitePositiveInteger(
    preview.obstacleCellCountX,
    Math.ceil(preview.worldWidth / preview.obstacleCellSize));
  preview.obstacleCellCountY = finitePositiveInteger(
    preview.obstacleCellCountY,
    Math.ceil(preview.worldHeight / preview.obstacleCellSize));

  const obstacleCellCount = preview.obstacleCellCountX * preview.obstacleCellCountY;
  if (!Array.isArray(preview.obstacleCells) || preview.obstacleCells.length !== obstacleCellCount) {
    preview.obstacleCells = new Array(obstacleCellCount).fill(false);
  } else {
    preview.obstacleCells = preview.obstacleCells.map(Boolean);
  }

  preview.obstacleBlockedCellCount = preview.obstacleCells.reduce(
    (count, blocked) => count + (blocked ? 1 : 0),
    0);
  return preview;
}

function finitePositiveNumber(value, fallback) {
  const number = Number(value);
  if (Number.isFinite(number) && number > 0) {
    return number;
  }

  const fallbackNumber = Number(fallback);
  return Number.isFinite(fallbackNumber) && fallbackNumber > 0 ? fallbackNumber : 1;
}

function finitePositiveInteger(value, fallback) {
  const number = Math.ceil(Number(value));
  if (Number.isFinite(number) && number > 0) {
    return number;
  }

  const fallbackNumber = Math.ceil(Number(fallback));
  return Number.isFinite(fallbackNumber) && fallbackNumber > 0 ? fallbackNumber : 1;
}

function drawBiomePreview(preview) {
  if (biomePreviewCollapsed) {
    return;
  }

  const canvas = biomePreviewCanvas;
  const context = canvas.getContext("2d");
  const pixelRatio = window.devicePixelRatio || 1;
  const body = canvas.parentElement;
  const bodyStyle = body ? window.getComputedStyle(body) : null;
  const columnCount = bodyStyle
    ? bodyStyle.gridTemplateColumns.split(" ").filter(Boolean).length
    : 1;
  const gapWidth = bodyStyle ? Number.parseFloat(bodyStyle.columnGap) || 0 : 0;
  const legendWidth = columnCount > 1 && biomePreviewLegend
    ? biomePreviewLegend.offsetWidth + gapWidth
    : 0;
  const availableWidth = Math.max(260, (body?.clientWidth || 720) - legendWidth);
  const maxWidth = Math.min(980, availableWidth);
  const maxHeight = 680;
  const worldWidth = Math.max(1, preview.worldWidth);
  const worldHeight = Math.max(1, preview.worldHeight);
  const scale = Math.min(maxWidth / worldWidth, maxHeight / worldHeight);
  const cssWidth = Math.max(1, worldWidth * scale);
  const cssHeight = Math.max(1, worldHeight * scale);
  canvas.style.width = `${Math.round(cssWidth)}px`;
  canvas.style.height = `${Math.round(cssHeight)}px`;
  canvas.width = Math.round(cssWidth * pixelRatio);
  canvas.height = Math.round(cssHeight * pixelRatio);
  context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
  context.clearRect(0, 0, cssWidth, cssHeight);
  context.fillStyle = "#f7f9f4";
  context.fillRect(0, 0, cssWidth, cssHeight);

  const colorByBiome = new Map((preview.biomes || []).map((biome) => [biome.name, biome.color]));
  const scaleX = cssWidth / Math.max(1, preview.worldWidth);
  const scaleY = cssHeight / Math.max(1, preview.worldHeight);
  for (let y = 0; y < preview.cellCountY; y++) {
    for (let x = 0; x < preview.cellCountX; x++) {
      const index = y * preview.cellCountX + x;
      const biome = preview.cells[index];
      context.fillStyle = colorByBiome.get(biome) || "#58ad57";
      const worldX = x * preview.cellSize;
      const worldY = y * preview.cellSize;
      const worldWidth = Math.min(preview.cellSize, preview.worldWidth - worldX);
      const worldHeight = Math.min(preview.cellSize, preview.worldHeight - worldY);
      context.fillRect(
        Math.floor(worldX * scaleX),
        Math.floor(worldY * scaleY),
        Math.ceil(worldWidth * scaleX) + 0.5,
        Math.ceil(worldHeight * scaleY) + 0.5);
    }
  }

  drawObstacleOverlay(context, preview, scaleX, scaleY);

  if (preview.resourceVoidBorderWidth > 0
      && preview.resourceVoidBorderWidth * 2 < preview.worldWidth
      && preview.resourceVoidBorderWidth * 2 < preview.worldHeight) {
    context.save();
    context.strokeStyle = "rgba(29, 37, 43, 0.72)";
    context.lineWidth = 2;
    context.setLineDash([8, 6]);
    const border = preview.resourceVoidBorderWidth;
    context.strokeRect(
      border * scaleX,
      border * scaleY,
      (preview.worldWidth - border * 2) * scaleX,
      (preview.worldHeight - border * 2) * scaleY);
    context.restore();
  }
}

function drawObstacleOverlay(context, preview, scaleX, scaleY) {
  if (!preview?.obstacleCells?.length || preview.obstacleBlockedCellCount <= 0) {
    return;
  }

  context.save();
  context.fillStyle = "rgba(6, 9, 8, 0.84)";
  context.strokeStyle = "rgba(255, 127, 0, 0.95)";
  for (let y = 0; y < preview.obstacleCellCountY; y++) {
    for (let x = 0; x < preview.obstacleCellCountX; x++) {
      const index = y * preview.obstacleCellCountX + x;
      if (!preview.obstacleCells[index]) {
        continue;
      }

      const worldX = x * preview.obstacleCellSize;
      const worldY = y * preview.obstacleCellSize;
      const worldWidth = Math.min(preview.obstacleCellSize, preview.worldWidth - worldX);
      const worldHeight = Math.min(preview.obstacleCellSize, preview.worldHeight - worldY);
      const left = Math.floor(worldX * scaleX);
      const top = Math.floor(worldY * scaleY);
      const width = Math.max(1, Math.ceil(worldWidth * scaleX));
      const height = Math.max(1, Math.ceil(worldHeight * scaleY));
      context.fillRect(left, top, width, height);
      if (width >= 5 && height >= 5) {
        context.lineWidth = 1;
        context.strokeRect(left + 0.5, top + 0.5, Math.max(0, width - 1), Math.max(0, height - 1));
      }
    }
  }

  context.restore();
}

function renderBiomePreviewLegend(preview) {
  const biomes = preview.biomes || [];
  const biomeItems = biomes.map((biome) => `
    <button class="biome-preview-legend-item${biomeBrushSelect?.value === biome.name ? " is-active" : ""}" type="button" data-biome="${escapeHtml(biome.name)}">
      <span class="biome-preview-swatch" style="background:${escapeHtml(biome.color)}"></span>
      ${escapeHtml(biome.name)} ${escapeHtml(formatPercent(biome.areaShare))}
    </button>
  `);
  const obstacleShare = preview.obstacleCells?.length
    ? preview.obstacleBlockedCellCount / preview.obstacleCells.length
    : 0;
  const obstacleItem = `
    <span class="biome-preview-legend-item">
      <span class="biome-preview-swatch obstacle-swatch"></span>
      Walls ${escapeHtml(formatPercent(obstacleShare))}
    </span>
  `;
  biomePreviewLegend.innerHTML = [...biomeItems, obstacleItem].join("");
}

function renderBiomeBrushOptions(preview) {
  if (!biomeBrushSelect) {
    return;
  }

  const previous = biomeBrushSelect.value;
  const biomes = preview?.biomes || [];
  biomeBrushSelect.innerHTML = "";
  for (const biome of biomes) {
    const option = document.createElement("option");
    option.value = biome.name;
    option.textContent = biome.name;
    biomeBrushSelect.append(option);
  }

  if (biomes.some((biome) => biome.name === previous)) {
    biomeBrushSelect.value = previous;
  } else if (biomes.length > 0) {
    biomeBrushSelect.value = biomes[0].name;
  }
}

function setBiomeBrush(biome) {
  if (!biomeBrushSelect || !biome) {
    return;
  }

  if ([...biomeBrushSelect.options].some((option) => option.value === biome)) {
    biomeBrushSelect.value = biome;
    if (currentBiomePreview) {
      renderBiomePreviewLegend(currentBiomePreview);
    }
  }
}

function paintBiomeCellAtEvent(event) {
  if (!biomePaintEnabled || !currentBiomePreview) {
    return;
  }

  if (paintLayerSelect?.value === "obstacle") {
    paintObstacleCellAtEvent(event);
    return;
  }

  if (!biomeBrushSelect?.value) {
    return;
  }

  const cell = biomeCellFromPointerEvent(event);
  if (!cell) {
    return;
  }

  const index = cell.y * currentBiomePreview.cellCountX + cell.x;
  if (currentBiomePreview.cells[index] === biomeBrushSelect.value) {
    return;
  }

  currentBiomePreview.cells[index] = biomeBrushSelect.value;
  recomputeBiomePreviewSummaries(currentBiomePreview);
  biomePaintDirty = true;
  drawBiomePreview(currentBiomePreview);
  renderBiomePreviewLegend(currentBiomePreview);
  updateBiomePaintControls();
  biomePreviewStatus.textContent = "Manual map edits are unsaved.";
}

function paintObstacleCellAtEvent(event) {
  if (!currentBiomePreview?.obstacleCells?.length) {
    return;
  }

  const cell = obstacleCellFromPointerEvent(event);
  if (!cell) {
    return;
  }

  const index = cell.y * currentBiomePreview.obstacleCellCountX + cell.x;
  const blocked = obstacleBrushSelect?.value !== "erase";
  if (currentBiomePreview.obstacleCells[index] === blocked) {
    return;
  }

  currentBiomePreview.obstacleCells[index] = blocked;
  currentBiomePreview.obstacleBlockedCellCount += blocked ? 1 : -1;
  currentBiomePreview.obstacleBlockedCellCount = Math.max(
    0,
    Math.min(currentBiomePreview.obstacleCells.length, currentBiomePreview.obstacleBlockedCellCount));
  biomePaintDirty = true;
  drawBiomePreview(currentBiomePreview);
  renderBiomePreviewLegend(currentBiomePreview);
  updateBiomePaintControls();
  biomePreviewStatus.textContent = "Manual map edits are unsaved.";
}

function biomeCellFromPointerEvent(event) {
  const rect = biomePreviewCanvas.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) {
    return null;
  }

  const x = event.clientX - rect.left;
  const y = event.clientY - rect.top;
  if (x < 0 || y < 0 || x > rect.width || y > rect.height) {
    return null;
  }

  const worldX = x / rect.width * currentBiomePreview.worldWidth;
  const worldY = y / rect.height * currentBiomePreview.worldHeight;
  return {
    x: Math.max(0, Math.min(currentBiomePreview.cellCountX - 1, Math.floor(worldX / currentBiomePreview.cellSize))),
    y: Math.max(0, Math.min(currentBiomePreview.cellCountY - 1, Math.floor(worldY / currentBiomePreview.cellSize)))
  };
}

function obstacleCellFromPointerEvent(event) {
  const rect = biomePreviewCanvas.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) {
    return null;
  }

  const x = event.clientX - rect.left;
  const y = event.clientY - rect.top;
  if (x < 0 || y < 0 || x > rect.width || y > rect.height) {
    return null;
  }

  const worldX = x / rect.width * currentBiomePreview.worldWidth;
  const worldY = y / rect.height * currentBiomePreview.worldHeight;
  return {
    x: Math.max(0, Math.min(currentBiomePreview.obstacleCellCountX - 1, Math.floor(worldX / currentBiomePreview.obstacleCellSize))),
    y: Math.max(0, Math.min(currentBiomePreview.obstacleCellCountY - 1, Math.floor(worldY / currentBiomePreview.obstacleCellSize)))
  };
}

function recomputeBiomePreviewSummaries(preview) {
  const countByBiome = new Map((preview.biomes || []).map((biome) => [biome.name, 0]));
  const areaByBiome = new Map((preview.biomes || []).map((biome) => [biome.name, 0]));
  for (let y = 0; y < preview.cellCountY; y++) {
    for (let x = 0; x < preview.cellCountX; x++) {
      const index = y * preview.cellCountX + x;
      const biome = preview.cells[index];
      const worldX = x * preview.cellSize;
      const worldY = y * preview.cellSize;
      const cellWidth = Math.max(0, Math.min(preview.cellSize, preview.worldWidth - worldX));
      const cellHeight = Math.max(0, Math.min(preview.cellSize, preview.worldHeight - worldY));
      countByBiome.set(biome, (countByBiome.get(biome) || 0) + 1);
      areaByBiome.set(biome, (areaByBiome.get(biome) || 0) + cellWidth * cellHeight);
    }
  }

  const worldArea = Math.max(1, preview.worldWidth * preview.worldHeight);
  for (const biome of preview.biomes || []) {
    biome.cellCount = countByBiome.get(biome.name) || 0;
    biome.areaShare = (areaByBiome.get(biome.name) || 0) / worldArea;
  }
}

function beginBiomePaint(event) {
  if (!biomePaintEnabled) {
    return;
  }

  event.preventDefault();
  biomePaintPointerDown = true;
  biomePreviewCanvas.setPointerCapture?.(event.pointerId);
  paintBiomeCellAtEvent(event);
}

function continueBiomePaint(event) {
  if (!biomePaintPointerDown) {
    return;
  }

  event.preventDefault();
  paintBiomeCellAtEvent(event);
}

function endBiomePaint(event) {
  if (!biomePaintPointerDown) {
    return;
  }

  biomePaintPointerDown = false;
  biomePreviewCanvas.releasePointerCapture?.(event.pointerId);
}

async function saveMapArtifact() {
  const result = await promptAndSaveMapArtifact();
  if (result) {
    formMessage.textContent = `Saved reusable map ${result.map.name} at ${result.worldMapPath}. Save the scenario to keep using it.`;
  }
}

async function promptAndSaveMapArtifact() {
  if (!currentBiomePreview || !scenarioEditor) {
    formMessage.textContent = "Render a map preview before saving a reusable map.";
    return null;
  }

  let scenario;
  try {
    scenario = collectScenarioOptions();
  } catch (error) {
    formMessage.textContent = error.message;
    return null;
  }

  const currentName = scenario?.name || selectedScenarioOption()?.name || "Reusable Map";
  const name = prompt("Save reusable map as", `${currentName} Map`);
  if (name === null) {
    return null;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    formMessage.textContent = "Map name is required.";
    return null;
  }

  return saveMapArtifactFromScenario(trimmedName, scenario);
}

async function saveMapArtifactFromScenario(name, scenario) {
  saveMapArtifactButton.disabled = true;
  formMessage.textContent = "Saving reusable map...";
  const response = await fetch("/api/map-artifacts", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      name,
      scenario,
      seed: valueOrNull("#seed"),
      scenarioPath: scenarioSelect.value,
      cells: currentBiomePreview.cells,
      obstacleCells: currentBiomePreview.obstacleCells
    })
  });

  if (!response.ok) {
    formMessage.textContent = await responseErrorMessage(
      response,
      "Reusable map save failed.",
      response.status === 405
        ? "The running Runner backend does not have the reusable map endpoint yet. Restart Lineage Runner and refresh this page."
        : null);
    updateBiomePaintControls();
    return null;
  }

  const result = await response.json();
  await loadMapArtifacts(result.worldMapPath);
  applyMapArtifactToScenario(result.map);
  biomePaintDirty = false;
  setBiomePaintEnabled(false);
  return result;
}

function applySelectedMapArtifact() {
  if (!scenarioEditor || !mapArtifactSelect?.value) {
    return;
  }

  if (biomePaintDirty && !confirm("Discard unsaved map edits and apply the selected reusable map?")) {
    return;
  }

  const map = mapArtifacts.find((candidate) => candidate.path === mapArtifactSelect.value);
  if (!map) {
    formMessage.textContent = "Selected reusable map was not found.";
    updateBiomePaintControls();
    return;
  }

  applyMapArtifactToScenario(map);
  biomePaintDirty = false;
  setBiomePaintEnabled(false);
  formMessage.textContent = `Applied reusable map ${map.name}. Save the scenario to keep using it.`;
}

function applyMapArtifactToScenario(map) {
  if (!scenarioEditor || !map) {
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    formMessage.textContent = error.message;
    return;
  }

  scenarioEditor.scenario.worldWidth = map.worldWidth;
  scenarioEditor.scenario.worldHeight = map.worldHeight;
  scenarioEditor.scenario.biomeCellSize = map.biomeCellSize;
  scenarioEditor.scenario.resourceVoidBorderWidth = map.resourceVoidBorderWidth;
  scenarioEditor.scenario.enableBiomes = true;
  scenarioEditor.scenario.biomeMapKind = "manual";
  scenarioEditor.scenario.worldMapPath = map.path;
  scenarioEditor.scenario.manualBiomeMapPath = null;

  scenarioEditor.scenario.obstacleCellSize = map.obstacleCellSize;
  scenarioEditor.scenario.manualObstacleMapPath = null;
  if (Number(map.obstacleBlockedCellCount || 0) > 0) {
    scenarioEditor.scenario.enableObstacles = true;
    scenarioEditor.scenario.obstacleMapKind = "manual";
  } else {
    scenarioEditor.scenario.enableObstacles = false;
    scenarioEditor.scenario.obstacleMapKind = "none";
  }

  activeScenarioGroup = "World & Terrain";
  renderScenarioEditor();
  renderMapArtifactDetails();
  scheduleBiomePreview(0);
}

async function renameSelectedMapArtifact() {
  const map = selectedMapArtifact();
  if (!map?.canDelete) {
    formMessage.textContent = "Selected map cannot be renamed.";
    return;
  }

  const name = prompt("Rename reusable map", map.name);
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    formMessage.textContent = "Map name is required.";
    return;
  }

  formMessage.textContent = "Renaming reusable map...";
  const response = await fetch("/api/map-artifacts/rename", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ path: map.path, name: trimmedName })
  });

  if (!response.ok) {
    formMessage.textContent = await responseErrorMessage(response, "Map rename failed.");
    updateBiomePaintControls();
    return;
  }

  const result = await response.json();
  updateScenarioMapReference(map.path, result.worldMapPath);
  await loadMapArtifacts(result.worldMapPath);
  formMessage.textContent = `Renamed reusable map to ${result.map.name}.`;
}

async function duplicateSelectedMapArtifact() {
  const map = selectedMapArtifact();
  if (!map?.canDelete) {
    formMessage.textContent = "Selected map cannot be duplicated.";
    return;
  }

  const name = prompt("Duplicate reusable map as", `${map.name} Copy`);
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    formMessage.textContent = "Map name is required.";
    return;
  }

  formMessage.textContent = "Duplicating reusable map...";
  const response = await fetch("/api/map-artifacts/duplicate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ path: map.path, name: trimmedName })
  });

  if (!response.ok) {
    formMessage.textContent = await responseErrorMessage(response, "Map duplicate failed.");
    updateBiomePaintControls();
    return;
  }

  const result = await response.json();
  await loadMapArtifacts(result.worldMapPath);
  formMessage.textContent = `Duplicated reusable map as ${result.map.name}.`;
}

async function deleteSelectedMapArtifact() {
  const map = selectedMapArtifact();
  if (!map?.canDelete) {
    formMessage.textContent = "Selected map cannot be deleted.";
    return;
  }

  const isReferenced = scenarioEditor?.scenario?.worldMapPath === map.path;
  const referenceWarning = isReferenced
    ? " The current scenario uses this map; deleting it will clear the scenario's reusable map path."
    : "";
  if (!confirm(`Archive reusable map "${map.name}"?${referenceWarning}`)) {
    return;
  }

  formMessage.textContent = "Archiving reusable map...";
  const response = await fetch("/api/map-artifacts/delete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ path: map.path })
  });

  if (!response.ok) {
    formMessage.textContent = await responseErrorMessage(response, "Map delete failed.");
    updateBiomePaintControls();
    return;
  }

  const result = await response.json();
  if (isReferenced) {
    clearScenarioMapReference();
    renderScenarioEditor();
    scheduleBiomePreview(0);
  }

  await loadMapArtifacts("");
  formMessage.textContent = `Archived reusable map ${map.name} to ${result.archivedPath}.`;
}

function updateScenarioMapReference(oldPath, newPath) {
  if (!scenarioEditor?.scenario || scenarioEditor.scenario.worldMapPath !== oldPath) {
    return;
  }

  scenarioEditor.scenario.worldMapPath = newPath;
  renderScenarioEditor();
  scheduleBiomePreview(0);
}

function clearScenarioMapReference() {
  if (!scenarioEditor?.scenario) {
    return;
  }

  scenarioEditor.scenario.worldMapPath = null;
  if (scenarioEditor.scenario.biomeMapKind === "manual") {
    scenarioEditor.scenario.biomeMapKind = "naturalClimate";
  }

  if (scenarioEditor.scenario.obstacleMapKind === "manual") {
    scenarioEditor.scenario.enableObstacles = false;
    scenarioEditor.scenario.obstacleMapKind = "none";
  }
}

async function responseErrorMessage(response, fallback, hint = null) {
  let detail = "";
  const contentType = response.headers.get("content-type") || "";
  if (contentType.includes("application/json")) {
    const problem = await response.json().catch(() => null);
    detail = problem?.error || problem?.title || "";
  } else {
    detail = (await response.text().catch(() => "")).trim();
  }

  const status = response.status ? `HTTP ${response.status}` : "";
  return [
    fallback,
    status,
    detail,
    hint
  ].filter(Boolean).join(" ");
}

function resetScenarioOptionFilters() {
  scenarioOptionSearch.value = "";
  const basicScope = scenarioScopeInputs.find((input) => input.value === "basic");
  if (basicScope) {
    basicScope.checked = true;
  }
}

function createScenarioField(field) {
  const wrapper = document.createElement("label");
  wrapper.className = [
    "scenario-field",
    `scenario-field-${field.type}`,
    field.advanced ? "is-advanced" : "",
    isScenarioFieldChanged(field) ? "is-changed" : ""
  ].filter(Boolean).join(" ");

  const label = document.createElement("span");
  label.className = "scenario-field-label";
  label.textContent = field.units ? `${field.label} (${field.units})` : field.label;
  wrapper.append(label);

  if (isScenarioFieldChanged(field)) {
    const changed = document.createElement("span");
    changed.className = "scenario-field-changed";
    changed.textContent = "changed";
    wrapper.append(changed);
  }

  wrapper.append(createScenarioControl(field));
  if (field.description) {
    const description = document.createElement("span");
    description.className = "scenario-field-description";
    description.textContent = field.description;
    wrapper.append(description);
  }

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
      control.step = field.step ?? "any";
      if (field.minimum !== null && field.minimum !== undefined) {
        control.min = field.minimum;
      }

      if (field.maximum !== null && field.maximum !== undefined) {
        control.max = field.maximum;
      }
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

function updateScenarioEditorStatus() {
  if (!scenarioEditor) {
    updateScenarioResetButtons();
    return;
  }

  const visibleCount = filteredScenarioFields().length;
  const totalCount = scenarioEditor.fields.length;
  const changedCount = changedScenarioFields().length;
  scenarioOptionsStatus.textContent = changedCount === 0
    ? `${visibleCount} of ${totalCount} options`
    : `${visibleCount} of ${totalCount} options, ${changedCount} changed`;
  updateScenarioResetButtons();
  updateScenarioManagementButtons();
}

function updateScenarioResetButtons() {
  if (!scenarioEditor || !scenarioEditorBaseline) {
    resetScenarioGroupButton.disabled = true;
    resetScenarioAllButton.disabled = true;
    return;
  }

  resetScenarioGroupButton.disabled = !scenarioEditor.fields.some((field) =>
    field.group === activeScenarioGroup && isScenarioFieldChanged(field));
  resetScenarioAllButton.disabled = changedScenarioFields().length === 0;
}

function updateScenarioFieldChangeMarkers() {
  for (const fieldNode of scenarioFields.querySelectorAll(".scenario-field")) {
    const control = fieldNode.querySelector(".scenario-control");
    const field = scenarioEditor?.fields.find((candidate) => candidate.jsonName === control?.dataset.jsonName);
    if (!field) {
      continue;
    }

    const isChanged = isScenarioFieldChanged(field);
    fieldNode.classList.toggle("is-changed", isChanged);
    const existingChanged = fieldNode.querySelector(".scenario-field-changed");
    if (isChanged && !existingChanged) {
      const changed = document.createElement("span");
      changed.className = "scenario-field-changed";
      changed.textContent = "changed";
      fieldNode.insertBefore(changed, control);
    } else if (!isChanged && existingChanged) {
      existingChanged.remove();
    }
  }

  updateScenarioEditorStatus();
}

function changedScenarioFields() {
  if (!scenarioEditor) {
    return [];
  }

  return scenarioEditor.fields.filter(isScenarioFieldChanged);
}

function isScenarioFieldChanged(field) {
  if (!scenarioEditor || !scenarioEditorBaseline) {
    return false;
  }

  return stableStringify(scenarioEditor.scenario?.[field.jsonName])
    !== stableStringify(scenarioEditorBaseline?.[field.jsonName]);
}

function resetActiveScenarioGroup() {
  if (!scenarioEditor || !scenarioEditorBaseline || !activeScenarioGroup) {
    return;
  }

  for (const field of scenarioEditor.fields.filter((candidate) => candidate.group === activeScenarioGroup)) {
    scenarioEditor.scenario[field.jsonName] = cloneJson(scenarioEditorBaseline[field.jsonName]);
  }

  renderScenarioEditor();
  scheduleBiomePreview();
}

function resetAllScenarioOptions() {
  if (!scenarioEditor || !scenarioEditorBaseline) {
    return;
  }

  scenarioEditor.scenario = cloneJson(scenarioEditorBaseline);
  appliedRecipes = [];
  recipeBaseScenario = cloneJson(scenarioEditorBaseline);
  recipeDiffCheckpoint = cloneJson(scenarioEditorBaseline);
  renderScenarioEditor();
  renderRecipeStack();
  scheduleBiomePreview();
}

async function saveScenarioAs() {
  if (!scenarioEditor) {
    return;
  }

  if (biomePaintDirty && currentBiomePreview) {
    const savedMap = await promptAndSaveMapArtifact();
    if (!savedMap) {
      return;
    }
  }

  let scenario;
  try {
    scenario = collectScenarioOptions();
  } catch (error) {
    formMessage.textContent = error.message;
    return;
  }

  const currentName = scenario?.name || selectedScenarioOption()?.name || "New Scenario";
  const name = prompt("Save scenario as", currentName.replace(/\s+(?:\[[^\]]+\]\s*)?\(.+\)$/u, ""));
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    formMessage.textContent = "Scenario name is required.";
    return;
  }

  saveScenarioButton.disabled = true;
  formMessage.textContent = "Saving scenario...";
  const response = await fetch("/api/scenarios/user", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name: trimmedName, scenario })
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Scenario save failed." }));
    formMessage.textContent = problem.error || "Scenario save failed.";
    updateScenarioManagementButtons();
    return;
  }

  const result = await response.json();
  await loadScenarios(result.scenario.path);
  scenarioOptionsPanel.hidden = false;
  formMessage.textContent = `Saved scenario ${result.scenario.name}.`;
}

async function deleteSelectedScenario() {
  const option = selectedScenarioOption();
  if (!option?.canDelete) {
    formMessage.textContent = "Built-in scenarios and manually added scenario files are protected.";
    return;
  }

  if (!confirm(`Archive saved scenario "${option.name}"?`)) {
    return;
  }

  deleteScenarioButton.disabled = true;
  formMessage.textContent = "Archiving scenario...";
  const response = await fetch(`/api/scenarios?path=${encodeURIComponent(option.path)}`, {
    method: "DELETE"
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Scenario delete failed." }));
    formMessage.textContent = problem.error || "Scenario delete failed.";
    updateScenarioManagementButtons();
    return;
  }

  const result = await response.json();
  await loadScenarios();
  formMessage.textContent = result.archivedPath
    ? `Archived scenario to ${result.archivedPath}.`
    : "Removed missing saved scenario from the launcher registry.";
}

function cloneJson(value) {
  return value === undefined ? undefined : JSON.parse(JSON.stringify(value));
}

function stableStringify(value) {
  return JSON.stringify(canonicalizeJson(value));
}

function canonicalizeJson(value) {
  if (Array.isArray(value)) {
    return value.map(canonicalizeJson);
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(Object.keys(value)
      .sort()
      .map((key) => [key, canonicalizeJson(value[key])]));
  }

  return value;
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

function addSelectedSpeciesToScenario() {
  if (!scenarioEditor) {
    formMessage.textContent = "Choose a scenario before adding a species profile.";
    return;
  }

  const species = selectedSpeciesCatalogEntry();
  if (!species) {
    formMessage.textContent = "Choose a species profile first.";
    return;
  }

  let count = Number(speciesSeedCountInput?.value || 0);
  if (!Number.isFinite(count) || count <= 0) {
    count = 10;
  }

  const energyRaw = speciesSeedEnergyInput?.value.trim() || "";
  const energyOverride = energyRaw === "" ? null : Number(energyRaw);
  if (energyRaw !== "" && (!Number.isFinite(energyOverride) || energyOverride <= 0)) {
    formMessage.textContent = "Energy override must be blank or a positive number.";
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    formMessage.textContent = error.message;
    return;
  }

  const roster = Array.isArray(scenarioEditor.scenario.speciesSeeds)
    ? cloneJson(scenarioEditor.scenario.speciesSeeds)
    : [];
  const brainChoice = parseSpeciesBrainChoice(speciesSeedBrainSelect?.value || "profile");
  const selectedBrainProfile = brainChoice.brainProfilePath
    ? findBrainCatalogEntryByPath(brainChoice.brainProfilePath)
    : null;
  if (brainChoice.brainProfilePath && selectedBrainProfile?.isCompatible === false) {
    formMessage.textContent = `${selectedBrainProfile.name} is not compatible with the current runtime: ${brainCompatibilityStatus(selectedBrainProfile)}`;
    return;
  }

  roster.push({
    profilePath: species.path,
    count: Math.round(count),
    spawnRegion: speciesSeedRegionSelect?.value || "uniform",
    energyOverride,
    brainOverrideKind: brainChoice.brainOverrideKind,
    brainProfilePath: brainChoice.brainProfilePath,
    enabled: true
  });
  scenarioEditor.scenario.speciesSeeds = roster;

  renderScenarioEditor();
  updateScenarioManagementButtons();
  renderSpeciesRoster();
  formMessage.textContent = `Added ${species.name} to the scenario species roster with ${formatSpeciesBrainChoice(brainChoice.brainOverrideKind, brainChoice.brainProfilePath)}. Save the scenario or start a run to use it.`;
}

function formatSpeciesBrainChoice(brainOverrideKind, brainProfilePath = null) {
  if (brainProfilePath) {
    const brain = brainCatalog.find((candidate) => candidate.path === brainProfilePath);
    return `catalog brain ${brain?.name || brainProfilePath}`;
  }

  return brainOverrideKind ? `generated ${formatEnumLabel(brainOverrideKind)} brain` : "the profile/default brain";
}

async function deleteSelectedSpeciesCatalogEntry() {
  const species = selectedSpeciesCatalogEntry();
  if (!species?.canDelete) {
    refreshStatus.textContent = "Built-in species profiles are protected.";
    return;
  }

  if (!confirm(`Archive species profile "${species.name}"?`)) {
    return;
  }

  deleteSpeciesCatalogButton.disabled = true;
  refreshStatus.textContent = "Archiving species profile";
  const response = await fetch("/api/species-catalog/delete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ path: species.path })
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Species profile delete failed." }));
    refreshStatus.textContent = problem.error || "Species profile delete failed.";
    renderSpeciesCatalogDetails();
    return;
  }

  const result = await response.json();
  await loadSpeciesCatalog();
  refreshStatus.textContent = `Archived species profile to ${result.archivedPath}.`;
}

async function deleteSelectedBrainCatalogEntry() {
  const brain = selectedBrainCatalogEntry();
  if (!brain?.canDelete) {
    refreshStatus.textContent = "Built-in brain profiles are protected.";
    return;
  }

  if (!confirm(`Archive brain profile "${brain.name}"?`)) {
    return;
  }

  deleteBrainCatalogButton.disabled = true;
  refreshStatus.textContent = "Archiving brain profile";
  const response = await fetch("/api/brain-catalog/delete", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ path: brain.path })
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Brain profile delete failed." }));
    refreshStatus.textContent = problem.error || "Brain profile delete failed.";
    renderBrainCatalogDetails();
    return;
  }

  const result = await response.json();
  await loadBrainCatalog();
  refreshStatus.textContent = `Archived brain profile to ${result.archivedPath}.`;
}

async function saveSpeciesFromRun(id) {
  const run = allRuns.find((candidate) => candidate.id === id);
  const defaultName = `${run?.scenarioName || "Run"} survivor`;
  const name = prompt("Species profile name", defaultName);
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    refreshStatus.textContent = "Species profile name is required.";
    return;
  }

  const selector = prompt(
    "Optional selector: creature 123, founder 123, or cluster key. Leave blank for the dominant living lineage.",
    "");
  if (selector === null) {
    return;
  }

  const notes = prompt("Species notes", `Saved from run ${run?.name || id}.`);
  if (notes === null) {
    return;
  }

  const exportPairedBrain = confirm("Also save this representative's brain as a paired catalog brain and make it the species default?");
  let payload;
  try {
    payload = buildSpeciesExportPayload(trimmedName, notes, selector, exportPairedBrain);
  } catch (error) {
    refreshStatus.textContent = error.message;
    return;
  }

  refreshStatus.textContent = "Saving species profile";
  const response = await fetch(`/api/runs/${encodeURIComponent(id)}/species-exports`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Species export failed." }));
    refreshStatus.textContent = problem.error || "Species export failed.";
    return;
  }

  const result = await response.json();
  await loadSpeciesCatalog(result.species.path);
  if (result.brain) {
    await loadBrainCatalog(result.brain.path);
  }

  refreshStatus.textContent = result.brain
    ? `Saved species profile ${result.species.name} with paired brain ${result.brain.name}.`
    : `Saved species profile ${result.species.name}.`;
  document.querySelector(".species-panel")?.scrollIntoView({ behavior: "smooth", block: "start" });
}

async function saveBrainFromRun(id) {
  const run = allRuns.find((candidate) => candidate.id === id);
  const defaultName = `${run?.scenarioName || "Run"} brain`;
  const name = prompt("Brain profile name", defaultName);
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    refreshStatus.textContent = "Brain profile name is required.";
    return;
  }

  const selector = prompt("Optional creature id. Leave blank for the dominant living lineage representative.", "");
  if (selector === null) {
    return;
  }

  const notes = prompt("Brain notes", `Saved from run ${run?.name || id}.`);
  if (notes === null) {
    return;
  }

  let creatureId = null;
  const selectorText = selector.trim();
  if (selectorText) {
    const match = selectorText.match(/^(creature)?\s*[:# ]?\s*(\d+)$/i);
    if (!match) {
      refreshStatus.textContent = "Brain selector must be blank or a creature id.";
      return;
    }

    creatureId = Number(match[2]);
  }

  refreshStatus.textContent = "Saving brain profile";
  const response = await fetch(`/api/runs/${encodeURIComponent(id)}/brain-exports`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name: trimmedName, notes: notes.trim(), creatureId })
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Brain export failed." }));
    refreshStatus.textContent = problem.error || "Brain export failed.";
    return;
  }

  const result = await response.json();
  await loadBrainCatalog(result.brain.path);
  refreshStatus.textContent = `Saved brain profile ${result.brain.name}.`;
  document.querySelector(".brain-panel")?.scrollIntoView({ behavior: "smooth", block: "start" });
}

function buildSpeciesExportPayload(name, notes, selectorText, exportPairedBrain = false) {
  const payload = { name, notes: notes.trim(), exportPairedBrain };
  const selector = selectorText.trim();
  if (!selector) {
    return payload;
  }

  const match = selector.match(/^(creature|founder|cluster)\s*[:# ]\s*(.+)$/i);
  if (match) {
    const kind = match[1].toLowerCase();
    const value = match[2].trim();
    if (kind === "cluster") {
      payload.clusterKey = value;
      return payload;
    }

    const parsed = Number(value);
    if (!Number.isInteger(parsed) || parsed <= 0) {
      throw new Error(`${kind} selector must use a positive numeric id.`);
    }

    if (kind === "founder") {
      payload.founderId = parsed;
    } else {
      payload.creatureId = parsed;
    }

    return payload;
  }

  const numeric = Number(selector);
  if (Number.isInteger(numeric) && numeric > 0) {
    payload.creatureId = numeric;
    return payload;
  }

  payload.clusterKey = selector;
  return payload;
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
        ${run.scenarioSummary ? `<div class="run-sub">setup ${escapeHtml(formatScenarioInline(run.scenarioSummary))}</div>` : ""}
        <div class="run-sub">seed ${escapeHtml(formatSeed(run.seed))}</div>
        <div class="run-sub">${escapeHtml(run.id)}</div>
      </td>
      <td>
        <span class="run-state ${statusClass(run.status)}">${escapeHtml(run.status || "unknown")}</span>
        ${run.stopReason ? `<div class="run-sub">${escapeHtml(run.stopReason)}</div>` : ""}
        ${run.failureReason ? `<div class="run-error">${escapeHtml(run.failureReason)}</div>` : ""}
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
        <div class="artifact-size">${escapeHtml(formatArtifactTotalSize(run.artifactSizeBytes))}</div>
        <div class="run-sub">${formatNumber(run.artifactFileCount)} file${Number(run.artifactFileCount || 0) === 1 ? "" : "s"}</div>
        <div class="run-sub">${escapeHtml(run.runDirectory)}</div>
        ${run.latestCheckpointPath ? `<div class="run-sub">Latest: ${escapeHtml(run.latestCheckpointPath)}</div>` : ""}
      </td>
      <td>
        <div class="actions">
          <button class="secondary" data-action="clone" data-id="${escapeHtml(run.id)}">Clone Settings</button>
          <button class="secondary" data-action="rerun" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "disabled" : ""}>Rerun</button>
          <button class="secondary" data-action="continue" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "disabled" : ""}>Continue</button>
          <button class="secondary" data-action="details" data-id="${escapeHtml(run.id)}">${isExpanded ? "Hide" : "Details"}</button>
          <button class="secondary" data-action="rename" data-id="${escapeHtml(run.id)}">Rename</button>
          <button class="secondary" data-action="save-species" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "disabled" : ""}>Save Species</button>
          <button class="secondary" data-action="save-brain" data-id="${escapeHtml(run.id)}" ${run.isRunning ? "disabled" : ""}>Save Brain</button>
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
          <div><span>Brain / roster</span><strong>${escapeHtml(formatScenarioBrainOrRoster(details.run.scenarioSummary))}</strong></div>
          <div><span>Vision</span><strong>${escapeHtml(formatScenarioVision(details.run.scenarioSummary))}</strong></div>
          <div><span>World</span><strong>${escapeHtml(formatScenarioWorld(details.run.scenarioSummary))}</strong></div>
          <div><span>Resources</span><strong>${escapeHtml(formatScenarioResources(details.run.scenarioSummary))}</strong></div>
          <div><span>Stats</span><strong>${escapeHtml(details.run.statsPath)}</strong></div>
          <div><span>Snapshot</span><strong>${escapeHtml(details.run.snapshotPath)}</strong></div>
          <div><span>Launch scenario</span><strong>${escapeHtml(details.run.launchScenarioPath || "")}</strong></div>
          <div><span>Resolved scenario</span><strong>${escapeHtml(details.run.resolvedScenarioPath)}</strong></div>
        </div>
        ${renderRunScenarioRoster(details.run.scenarioSummary)}
        ${details.error ? `<div class="detail-error">${escapeHtml(details.error)}</div>` : ""}
        ${renderArtifactsPanel(details)}
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

function renderRunScenarioRoster(summary) {
  const seeds = scenarioEnabledSpeciesSeeds(summary);
  if (seeds.length === 0) {
    return "";
  }

  return `
    <div class="artifacts-panel">
      <div class="log-title">Starting roster</div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Profile</th>
              <th>Brain</th>
              <th>Count</th>
              <th>Region</th>
              <th>Energy</th>
            </tr>
          </thead>
          <tbody>
            ${seeds.map((seed) => `
              <tr>
                <td>${escapeHtml(seed.label || seed.profileName || seed.profilePath || "species")}<div class="run-sub">${escapeHtml(seed.profilePath || "")}</div></td>
                <td>${escapeHtml(seed.brain || "profile brain")}${seed.brainProfilePath ? `<div class="run-sub">${escapeHtml(seed.brainProfilePath)}</div>` : ""}</td>
                <td>${formatNumber(seed.count || 0)}</td>
                <td>${escapeHtml(formatEnumLabel(seed.spawnRegion || "uniform"))}</td>
                <td>${seed.energyOverride === null || seed.energyOverride === undefined ? "profile" : formatNumber(seed.energyOverride)}</td>
              </tr>
            `).join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;
}

function renderArtifactsPanel(details) {
  const artifacts = Array.isArray(details.artifacts) ? details.artifacts : [];
  if (artifacts.length === 0) {
    return `
      <div class="artifacts-panel">
        <div class="log-title">Artifacts</div>
        <div class="run-sub">No artifacts have been recorded for this run yet.</div>
      </div>
    `;
  }

  const rows = artifacts.map((artifact) => {
    const canContinue = artifact.exists && artifact.isContinuationSource && !details.run.isRunning;
    const canOpenReport = artifact.exists && artifact.type === "report";
    return `
      <tr>
        <td>
          <div class="artifact-label">${escapeHtml(artifact.label)}</div>
          <div class="run-sub">${escapeHtml(artifact.type)}${artifact.isLatestCheckpoint ? " - latest" : ""}</div>
        </td>
        <td>${artifact.tick === null || artifact.tick === undefined ? "" : formatNumber(artifact.tick)}</td>
        <td>${escapeHtml(formatFileSize(artifact.sizeBytes))}</td>
        <td>${escapeHtml(formatDateTime(artifact.modifiedAtUtc))}</td>
        <td class="artifact-path">${escapeHtml(artifact.path)}</td>
        <td>
          <div class="artifact-actions">
            <button class="secondary" data-action="copy-path" data-path="${escapeHtml(artifact.path)}" ${artifact.path ? "" : "disabled"}>Copy Path</button>
            ${canOpenReport ? `<button class="secondary" data-action="report" data-id="${escapeHtml(details.run.id)}">Open</button>` : ""}
            ${canContinue ? `<button class="secondary" data-action="continue-artifact" data-id="${escapeHtml(details.run.id)}" data-path="${escapeHtml(artifact.path)}">Continue</button>` : ""}
          </div>
        </td>
      </tr>
    `;
  }).join("");

  return `
    <div class="artifacts-panel">
      <div class="log-title">Artifacts / checkpoints</div>
      <div class="artifacts-scroll">
        <table class="artifacts-table">
          <thead>
            <tr>
              <th>Artifact</th>
              <th>Tick</th>
              <th>Size</th>
              <th>Modified</th>
              <th>Path</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>${rows}</tbody>
        </table>
      </div>
    </div>
  `;
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
      run.failureReason,
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
    case "artifactSize":
      return Number(run.artifactSizeBytes || 0);
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

  if (action === "save-species") {
    await saveSpeciesFromRun(id);
    return;
  }

  if (action === "save-brain") {
    await saveBrainFromRun(id);
    return;
  }

  if (action === "clone") {
    await cloneRunSettings(id);
    return;
  }

  if (action === "rerun") {
    await rerunRun(id);
    return;
  }

  if (action === "copy-path") {
    await copyText(button.dataset.path || "");
    return;
  }

  if (action === "continue-artifact") {
    await continueRun(id, button.dataset.path || null);
    return;
  }

  if (action === "continue") {
    await continueRun(id);
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

scenarioOptionSearch.addEventListener("input", () => {
  try {
    storeVisibleScenarioValues();
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
    return;
  }

  renderScenarioEditor();
});

for (const input of scenarioScopeInputs) {
  input.addEventListener("change", () => {
    try {
      storeVisibleScenarioValues();
    } catch (error) {
      scenarioOptionsStatus.textContent = error.message;
      return;
    }

    renderScenarioEditor();
  });
}

resetScenarioGroupButton.addEventListener("click", resetActiveScenarioGroup);
resetScenarioAllButton.addEventListener("click", resetAllScenarioOptions);
saveScenarioButton.addEventListener("click", saveScenarioAs);
deleteScenarioButton.addEventListener("click", deleteSelectedScenario);
recipePicker.addEventListener("input", () => {
  renderRecipePicker();
  updateScenarioManagementButtons();
});
applyRecipeButton.addEventListener("click", applySelectedRecipe);
archiveRecipeButton.addEventListener("click", archiveSelectedRecipe);
deleteRecipeButton.addEventListener("click", deleteSelectedRecipe);
reviewLaunchDiffButton.addEventListener("click", reviewLaunchDiff);
saveRecipeButton.addEventListener("click", saveCurrentDiffAsRecipe);
saveNewRecipeButton.addEventListener("click", saveNewDiffsAsRecipe);
confirmSaveRecipeButton.addEventListener("click", confirmPendingRecipeSave);
closeScenarioDiffButton.addEventListener("click", closeScenarioDiffPanel);

recipeStack.addEventListener("click", (event) => {
  const button = event.target.closest("button[data-recipe-action]");
  if (!button) {
    return;
  }

  updateRecipeStack(button.dataset.recipeAction, Number(button.dataset.index));
});

speciesCatalogSelect.addEventListener("change", renderSpeciesCatalogDetails);
refreshSpeciesCatalogButton.addEventListener("click", () => loadSpeciesCatalog());
deleteSpeciesCatalogButton.addEventListener("click", deleteSelectedSpeciesCatalogEntry);
addSpeciesToScenarioButton.addEventListener("click", addSelectedSpeciesToScenario);
if (speciesRosterDetails) {
  speciesRosterDetails.addEventListener("click", (event) => {
    const button = event.target.closest("button[data-species-roster-action]");
    if (!button) {
      return;
    }

    updateSpeciesRoster(button.dataset.speciesRosterAction, Number(button.dataset.index));
  });
  speciesRosterDetails.addEventListener("change", (event) => {
    const control = event.target.closest("[data-species-roster-field]");
    if (!control) {
      return;
    }

    updateSpeciesRosterField(control);
  });
}
brainCatalogSelect.addEventListener("change", renderBrainCatalogDetails);
refreshBrainCatalogButton.addEventListener("click", () => loadBrainCatalog());
deleteBrainCatalogButton.addEventListener("click", deleteSelectedBrainCatalogEntry);
refreshBrainLabSnapshotsButton.addEventListener("click", () => loadBrainLabSnapshots());
loadBrainLabSnapshotButton.addEventListener("click", loadBrainLabSnapshot);
evaluateBrainLabButton.addEventListener("click", evaluateBrainLab);
muteBrainLabSoundButton.addEventListener("click", muteBrainLabSound);
resetBrainLabOverridesButton.addEventListener("click", resetBrainLabOverrides);
brainLabSnapshotSelect.addEventListener("change", () => {
  brainLabSnapshotPath.value = brainLabSnapshotSelect.value;
  if (brainLabSnapshotSelect.value) {
    loadBrainLabSnapshot();
  }
});
brainLabCreatureSelect.addEventListener("change", () => {
  brainLabOverrides = {};
  evaluateBrainLab();
});
brainLabGroupFilter.addEventListener("change", renderBrainLabInputs);
brainLabInputs.addEventListener("input", (event) => {
  const control = event.target.closest("[data-brain-lab-input-key]");
  if (control) {
    updateBrainLabInputOverride(control);
  }
});
brainLabInputs.addEventListener("click", (event) => {
  const button = event.target.closest("[data-brain-lab-reset-input]");
  if (button) {
    resetBrainLabInput(button.dataset.brainLabResetInput);
  }
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
    updateScenarioFieldChangeMarkers();
    renderMapArtifactDetails();
    renderSpeciesRoster();
    scheduleBiomePreview();
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
  }
});

scenarioFields.addEventListener("input", () => {
  try {
    storeVisibleScenarioValues();
    updateScenarioFieldChangeMarkers();
    renderMapArtifactDetails();
    renderSpeciesRoster();
    scheduleBiomePreview();
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
  }
});

seedInput.addEventListener("input", () => scheduleBiomePreview());
toggleBiomePreviewButton.addEventListener("click", () => setBiomePreviewCollapsed(!biomePreviewCollapsed));
refreshBiomePreviewButton.addEventListener("click", () => scheduleBiomePreview(0));
paintBiomeMapButton.addEventListener("click", () => setBiomePaintEnabled(!biomePaintEnabled));
mapArtifactSelect.addEventListener("change", updateBiomePaintControls);
applyMapArtifactButton.addEventListener("click", applySelectedMapArtifact);
saveMapArtifactButton.addEventListener("click", saveMapArtifact);
renameMapArtifactButton.addEventListener("click", renameSelectedMapArtifact);
duplicateMapArtifactButton.addEventListener("click", duplicateSelectedMapArtifact);
deleteMapArtifactButton.addEventListener("click", deleteSelectedMapArtifact);
paintLayerSelect.addEventListener("change", () => {
  if (biomePaintEnabled) {
    setBiomePaintEnabled(true);
  } else {
    updateBiomePaintControls();
  }
  if (currentBiomePreview) {
    renderBiomePreviewLegend(currentBiomePreview);
  }
});
biomeBrushSelect.addEventListener("change", () => {
  if (currentBiomePreview) {
    renderBiomePreviewLegend(currentBiomePreview);
  }
});
obstacleBrushSelect.addEventListener("change", updateBiomePaintControls);
biomePreviewLegend.addEventListener("click", (event) => {
  const button = event.target.closest("button[data-biome]");
  if (button) {
    setBiomeBrush(button.dataset.biome);
  }
});
biomePreviewCanvas.addEventListener("pointerdown", beginBiomePaint);
biomePreviewCanvas.addEventListener("pointermove", continueBiomePaint);
biomePreviewCanvas.addEventListener("pointerup", endBiomePaint);
biomePreviewCanvas.addEventListener("pointercancel", endBiomePaint);
biomePreviewCanvas.addEventListener("lostpointercapture", () => {
  biomePaintPointerDown = false;
});
window.addEventListener("resize", () => {
  if (currentBiomePreview) {
    drawBiomePreview(currentBiomePreview);
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

async function cloneRunSettings(id) {
  refreshStatus.textContent = "Loading clone settings";
  const response = await fetch(`/api/runs/${encodeURIComponent(id)}/clone-settings`);
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Clone settings unavailable." }));
    refreshStatus.textContent = problem.error || "Clone settings unavailable.";
    return;
  }

  const settings = await response.json();
  ensureScenarioOption(settings.scenarioPath, settings.scenarioEditor?.scenario?.name || settings.scenarioPath);
  scenarioSelect.value = settings.scenarioPath;
  document.querySelector("#ticks").value = settings.ticks;
  document.querySelector("#seed").value = settings.seed ?? "";
  document.querySelector("#checkpointInterval").value = settings.checkpointIntervalTicks ?? "";
  document.querySelector("#stopOnExtinction").checked = Boolean(settings.stopOnExtinction);

  resetScenarioOptionFilters();
  loadScenarioEditorDefinition(settings.scenarioEditor);
  scenarioOptionsPanel.hidden = false;
  formMessage.textContent = `Loaded settings from ${settings.sourceRunName}.`;
  refreshStatus.textContent = "Clone settings loaded";
  document.querySelector(".launch-panel").scrollIntoView({ behavior: "smooth", block: "start" });
}

async function rerunRun(id) {
  const run = allRuns.find((candidate) => candidate.id === id);
  const name = run?.name || id;
  if (!confirm(`Rerun "${name}" and overwrite its current results? This will delete the current run artifacts after the replacement starts.`)) {
    return;
  }

  refreshStatus.textContent = "Starting rerun";
  const response = await fetch(`/api/runs/${encodeURIComponent(id)}/rerun`, { method: "POST" });
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Rerun failed." }));
    refreshStatus.textContent = problem.error || "Rerun failed.";
    return;
  }

  const result = await response.json();
  selectedRunIds.delete(id);
  if (expandedRunId === id) {
    expandedRunId = null;
  }

  await loadRuns();
  refreshStatus.textContent = result.deletedOriginal
    ? `Rerun started: ${result.run.name}`
    : `Rerun started: ${result.run.name}; original artifacts were kept`;
}

async function continueRun(id, snapshotPath = null) {
  const run = allRuns.find((candidate) => candidate.id === id);
  const name = run?.name || id;
  const remainingTicks = Number(run?.ticks || 0) - Number(run?.completedSteps || 0);
  const ticks = remainingTicks > 0
    ? Math.ceil(remainingTicks)
    : Math.max(1, Number(run?.ticks || document.querySelector("#ticks").value || 20000));

  refreshStatus.textContent = snapshotPath
    ? `Continuing ${name} from selected snapshot for ${formatNumber(ticks)} tick(s)`
    : `Continuing ${name} for ${formatNumber(ticks)} tick(s)`;
  const payload = { ticks };
  if (snapshotPath) {
    payload.snapshotPath = snapshotPath;
  }

  const response = await fetch(`/api/runs/${encodeURIComponent(id)}/continue`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Continue failed." }));
    refreshStatus.textContent = problem.error || "Continue failed.";
    return;
  }

  const result = await response.json();
  await loadRuns();
  refreshStatus.textContent = `Continued from ${result.snapshotPath}: ${result.run.name}`;
}

async function copyText(text) {
  if (!text) {
    return;
  }

  try {
    await navigator.clipboard.writeText(text);
  } catch {
    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.setAttribute("readonly", "");
    textarea.style.position = "fixed";
    textarea.style.left = "-9999px";
    document.body.append(textarea);
    textarea.select();
    document.execCommand("copy");
    textarea.remove();
  }

  refreshStatus.textContent = "Path copied.";
}

function ensureScenarioOption(path, name) {
  if ([...scenarioSelect.options].some((option) => option.value === path)) {
    return;
  }

  const scenario = {
    name,
    path,
    isUserCreated: false,
    canDelete: false
  };
  scenarioOptions.push(scenario);
  const option = document.createElement("option");
  option.value = path;
  option.textContent = scenarioOptionLabel(scenario);
  option.dataset.isUserCreated = "false";
  option.dataset.canDelete = "false";
  scenarioSelect.append(option);
  updateScenarioManagementButtons();
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

function formatFileSize(value) {
  const bytes = Number(value || 0);
  if (!bytes) {
    return "";
  }

  if (bytes < 1024) {
    return `${bytes} B`;
  }

  const units = ["KB", "MB", "GB"];
  let scaled = bytes / 1024;
  let unitIndex = 0;
  while (scaled >= 1024 && unitIndex < units.length - 1) {
    scaled /= 1024;
    unitIndex++;
  }

  return `${scaled.toLocaleString(undefined, { maximumFractionDigits: 1 })} ${units[unitIndex]}`;
}

function formatArtifactTotalSize(value) {
  return formatFileSize(value) || "0 B";
}

function formatDownloadTimestamp(value) {
  return value.toISOString().slice(0, 19).replaceAll(":", "").replace("T", "-");
}

function formatScenarioInline(summary) {
  if (!summary) {
    return "";
  }

  return [
    formatScenarioBrainOrRoster(summary),
    formatScenarioWorld(summary),
    formatScenarioResources(summary)
  ].filter(Boolean).join(" | ");
}

function formatScenarioBrainOrRoster(summary) {
  if (scenarioEnabledSpeciesSeeds(summary).length > 0) {
    return `roster ${formatScenarioRosterBrief(summary)}`;
  }

  return formatScenarioBrain(summary);
}

function scenarioEnabledSpeciesSeeds(summary) {
  return Array.isArray(summary?.speciesSeeds)
    ? summary.speciesSeeds.filter((seed) => seed.enabled !== false)
    : [];
}

function formatScenarioRosterBrief(summary) {
  const seeds = scenarioEnabledSpeciesSeeds(summary);
  if (seeds.length === 0) {
    return "";
  }

  const total = seeds.reduce((sum, seed) => sum + Number(seed.count || 0), 0);
  return seeds.length === 1
    ? `${formatNumber(total)} x ${seeds[0].label || seeds[0].profileName || seeds[0].profilePath || "species"} using ${seeds[0].brain || "profile brain"}`
    : `${formatNumber(total)} creatures across ${formatNumber(seeds.length)} entries`;
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

  const seeds = scenarioEnabledSpeciesSeeds(summary);
  if (seeds.length > 0) {
    bits.push(`${formatNumber(seeds.reduce((sum, seed) => sum + Number(seed.count || 0), 0))} roster creatures`);
  } else if (summary.initialCreatureCount !== null && summary.initialCreatureCount !== undefined) {
      bits.push(`${formatNumber(summary.initialCreatureCount)} creatures`);
  }

  return bits.join(", ");
}

function formatDecimal(value) {
  return Number(value || 0).toLocaleString(undefined, { maximumFractionDigits: 3 });
}

function formatEnumLabel(value) {
  return String(value ?? "")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .replace(/\b\w/g, (letter) => letter.toUpperCase());
}

function formatPercent(value) {
  return `${(Number(value || 0) * 100).toLocaleString(undefined, { maximumFractionDigits: 1 })}%`;
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
  setBiomePreviewCollapsed(readBiomePreviewCollapsed(), false);
  await loadMapArtifacts();
  await loadBrainCatalog();
  await loadBrainLabSnapshots();
  await loadSpeciesCatalog();
  await loadScenarioRecipes();
  await loadScenarios();
  await loadRuns();
  refreshTimer = setInterval(loadRuns, 2000);
}

boot().catch((error) => {
  refreshStatus.textContent = "Error";
  formMessage.textContent = error.message;
});
