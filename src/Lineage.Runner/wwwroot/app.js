const scenarioSelect = document.querySelector("#scenarioPath");
const runsBody = document.querySelector("#runsBody");
const launchForm = document.querySelector("#launchForm");
const formMessage = document.querySelector("#formMessage");
const refreshStatus = document.querySelector("#refreshStatus");
const launcherTabs = document.querySelector("#launcherTabs");
const launcherTabButtons = [...document.querySelectorAll("[data-launcher-tab]")];
const launcherTabPanels = [...document.querySelectorAll("[data-launcher-panel]")];
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
const recipeDescription = document.querySelector("#recipeDescription");
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
const exportBrainLabSpeciesButton = document.querySelector("#exportBrainLabSpeciesButton");
const exportBrainLabBrainButton = document.querySelector("#exportBrainLabBrainButton");
const muteBrainLabSoundButton = document.querySelector("#muteBrainLabSoundButton");
const resetBrainLabOverridesButton = document.querySelector("#resetBrainLabOverridesButton");
const brainLabPresetSelect = document.querySelector("#brainLabPresetSelect");
const applyBrainLabPresetButton = document.querySelector("#applyBrainLabPresetButton");
const compareBrainLabPopulationButton = document.querySelector("#compareBrainLabPopulationButton");
const runBrainLabPresetMatrixButton = document.querySelector("#runBrainLabPresetMatrixButton");
const compareBrainLabProfilesButton = document.querySelector("#compareBrainLabProfilesButton");
const brainLabProfileScopeSelect = document.querySelector("#brainLabProfileScope");
const runBrainLabProbeTestsButton = document.querySelector("#runBrainLabProbeTestsButton");
const brainLabMeta = document.querySelector("#brainLabMeta");
const brainLabInputs = document.querySelector("#brainLabInputs");
const brainLabOutputs = document.querySelector("#brainLabOutputs");
const brainLabProbeTests = document.querySelector("#brainLabProbeTests");
const brainLabPopulation = document.querySelector("#brainLabPopulation");
const brainLabPresetMatrix = document.querySelector("#brainLabPresetMatrix");
const brainLabProfileComparison = document.querySelector("#brainLabProfileComparison");
const brainLabWorldProbe = document.querySelector("#brainLabWorldProbe");
const brainLabWorldProbeCanvas = document.querySelector("#brainLabWorldProbeCanvas");
const brainLabWorldProbeSummary = document.querySelector("#brainLabWorldProbeSummary");
const brainLabWorldProbeSelection = document.querySelector("#brainLabWorldProbeSelection");
const brainLabWorldProbeTrace = document.querySelector("#brainLabWorldProbeTrace");
const brainLabWorldProbeEditor = document.querySelector("#brainLabWorldProbeEditor");
const brainLabWorldProbeFixtureSelect = document.querySelector("#brainLabWorldProbeFixtureSelect");
const applyBrainLabWorldProbeFixtureButton = document.querySelector("#applyBrainLabWorldProbeFixtureButton");
const saveBrainLabWorldProbeFixtureButton = document.querySelector("#saveBrainLabWorldProbeFixtureButton");
const deleteBrainLabWorldProbeFixtureButton = document.querySelector("#deleteBrainLabWorldProbeFixtureButton");
const brainLabWorldProbeEnvironmentSelect = document.querySelector("#brainLabWorldProbeEnvironmentSelect");
const brainLabWorldProbeBiomeSelect = document.querySelector("#brainLabWorldProbeBiomeSelect");
const brainLabWorldProbeBoundarySelect = document.querySelector("#brainLabWorldProbeBoundarySelect");
const brainLabWorldProbeBoundaryOffsetInput = document.querySelector("#brainLabWorldProbeBoundaryOffsetInput");
const brainLabWorldProbeFertilityInput = document.querySelector("#brainLabWorldProbeFertilityInput");
const brainLabWorldProbeObstacleSelect = document.querySelector("#brainLabWorldProbeObstacleSelect");
const brainLabWorldProbeZoomOutButton = document.querySelector("#brainLabWorldProbeZoomOutButton");
const brainLabWorldProbeZoomInButton = document.querySelector("#brainLabWorldProbeZoomInButton");
const brainLabWorldProbeZoomResetButton = document.querySelector("#brainLabWorldProbeZoomResetButton");
const brainLabWorldProbeZoomStatus = document.querySelector("#brainLabWorldProbeZoomStatus");
const brainLabWorldProbeToolButtons = document.querySelectorAll("[data-brain-lab-world-tool]");
const hideBrainLabWorldProbeSelectionButton = document.querySelector("#hideBrainLabWorldProbeSelectionButton");
const muteBrainLabWorldProbeSelectionSoundButton = document.querySelector("#muteBrainLabWorldProbeSelectionSoundButton");
const clearBrainLabWorldProbeEditsButton = document.querySelector("#clearBrainLabWorldProbeEditsButton");
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

const ecologicalEventKinds = [
  ["regionalFertilityPulse", "Fertility pulse"],
  ["regionalFertilityCrash", "Fertility crash"],
  ["heatWave", "Heat wave"],
  ["coldSnap", "Cold snap"]
];

let refreshTimer = null;
let allRuns = [];
let scenarioOptions = [];
let mapArtifacts = [];
let scenarioRecipes = [];
let brainCatalog = [];
let speciesCatalog = [];
let brainLabSnapshots = [];
let brainLabWorldProbeFixtures = [];
let brainLabSnapshot = null;
let brainLabEvaluation = null;
let brainLabPopulationEvaluation = null;
let brainLabPresetMatrixResult = null;
let brainLabProbeTestResult = null;
let brainLabProfileComparisonResult = null;
let brainLabProfileComparisonCohortKey = null;
let brainLabProfileComparisonRunning = false;
let brainLabWorldProbeScene = null;
let brainLabWorldProbeBaseScene = null;
let brainLabOverrides = {};
let brainLabEvaluateTimer = null;
let brainLabWorldProbeOverrideKeys = new Set();
let brainLabWorldProbeHiddenKeys = new Set();
let brainLabWorldProbeMutedSoundKeys = new Set();
let brainLabWorldProbeSelected = null;
let brainLabSelectedInputKey = null;
let brainLabWorldProbeHitTargets = [];
let brainLabWorldProbeTool = "select";
let brainLabWorldProbeDrag = null;
let brainLabWorldProbeBoundaryDrag = null;
let brainLabWorldProbeEdited = false;
let brainLabWorldProbeNextSyntheticId = -1;
let brainLabWorldProbeZoom = 1;
let brainLabWorldProbePanX = 0;
let brainLabWorldProbePanY = 0;
let brainLabWorldProbePan = null;
let brainLabWorldProbeSuppressClick = false;
let appliedRecipes = [];
let recipeBaseScenario = null;
let recipeDiffCheckpoint = null;
let pendingRecipeSave = null;
let selectedRunIds = new Set();

const brainLabProfileComparisonBatchSize = 100;
const brainLabProfileComparisonSampleSize = 100;
const brainLabProfileComparisonSampleBatchSize = 25;

const brainLabBiomeColors = {
  Desert: "#c7b56f",
  Scrubland: "#8d8d49",
  Grassland: "#58ad57",
  Fertile: "#2f8f43",
  Forest: "#123d22",
  Wetland: "#2e8a8a",
  Tundra: "#b4c2c5",
  Highland: "#887b68"
};

const brainLabWorldProbeEnvironmentProfiles = {
  snapshot: { biomeKind: "snapshot", boundaryDirection: "none", boundaryOffset: 0, localFertility: "", obstacleMode: "snapshot" },
  grassland: { biomeKind: "Grassland", boundaryDirection: "none", boundaryOffset: 0, localFertility: 1, obstacleMode: "clear" },
  fertile: { biomeKind: "Fertile", boundaryDirection: "none", boundaryOffset: 0, localFertility: 1, obstacleMode: "clear" },
  desert: { biomeKind: "Desert", boundaryDirection: "none", boundaryOffset: 0, localFertility: 0.35, obstacleMode: "clear" },
  forest: { biomeKind: "Forest", boundaryDirection: "none", boundaryOffset: 0, localFertility: 0.85, obstacleMode: "clear" },
  wetland: { biomeKind: "Wetland", boundaryDirection: "none", boundaryOffset: 0, localFertility: 1, obstacleMode: "clear" },
  tundra: { biomeKind: "Tundra", boundaryDirection: "none", boundaryOffset: 0, localFertility: 0.45, obstacleMode: "clear" },
  highland: { biomeKind: "Highland", boundaryDirection: "none", boundaryOffset: 0, localFertility: 0.65, obstacleMode: "clear" },
  boundaryAhead: { biomeKind: "Desert", boundaryDirection: "forward", boundaryOffset: 0, localFertility: "", obstacleMode: "clear" }
};

let expandedRunId = null;
let runDetailsById = new Map();
let sortKey = "createdAtUtc";
let sortDirection = "desc";
let scenarioEditor = null;
let scenarioEditorBaseline = null;
let activeLauncherTab = "launch";
let activeScenarioGroup = null;
let biomePreviewTimer = null;
let biomePreviewRequestId = 0;
let currentBiomePreview = null;
let biomePreviewCollapsed = false;
let biomePaintEnabled = false;
let biomePaintDirty = false;
let biomePaintPointerDown = false;

function setLauncherTab(tab) {
  if (!launcherTabPanels.some((panel) => panel.dataset.launcherPanel === tab)) {
    return;
  }

  activeLauncherTab = tab;
  for (const button of launcherTabButtons) {
    const isActive = button.dataset.launcherTab === tab;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-selected", String(isActive));
    button.tabIndex = isActive ? 0 : -1;
  }

  for (const panel of launcherTabPanels) {
    const isActive = panel.dataset.launcherPanel === tab;
    panel.classList.toggle("is-active", isActive);
    panel.hidden = !isActive;
  }

  if (tab === "brain") {
    renderBrainLabWorldProbe();
  }
}

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
  renderSpeciesBrainChoiceOptions(defaultSpeciesBrainChoiceValue());
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

function renderSpeciesBrainChoiceOptions(selectedValue = speciesSeedBrainSelect?.value || defaultSpeciesBrainChoiceValue()) {
  if (!speciesSeedBrainSelect) {
    return;
  }

  const selected = normalizeBrainChoiceValue(selectedValue || "profile");
  const fallback = defaultSpeciesBrainChoiceValue();
  const missingBrainPath = selected.startsWith("catalog:") ? selected.slice("catalog:".length) : null;
  speciesSeedBrainSelect.innerHTML = brainChoiceOptionsHtml(selected, missingBrainPath, selectedSpeciesCatalogEntry());
  if (![...speciesSeedBrainSelect.options].some((option) => option.value === selected && !option.disabled)) {
    speciesSeedBrainSelect.value = [...speciesSeedBrainSelect.options].some((option) => option.value === fallback && !option.disabled)
      ? fallback
      : speciesSeedBrainSelect.options[0]?.value || "profile";
  } else {
    speciesSeedBrainSelect.value = selected;
  }
}

function brainChoiceOptionsHtml(selectedValue = "profile", missingBrainPath = null, species = selectedSpeciesCatalogEntry()) {
  const selected = normalizeBrainChoiceValue(selectedValue || "profile");
  const pieces = [];

  if (selected === "profile" || !species?.defaultBrainPath) {
    pieces.push(`<option value="profile"${selected === "profile" ? " selected" : ""}>${escapeHtml(species?.defaultBrainPath ? "Species default brain" : "Profile embedded brain")}</option>`);
  }

  if (selected.startsWith("generated:")) {
    const kind = selected.slice("generated:".length);
    pieces.push(`<option value="${escapeHtml(selected)}" selected>Legacy generated ${escapeHtml(formatEnumLabel(kind))} brain</option>`);
  }

  if (brainCatalog.length > 0 || missingBrainPath) {
    for (const brain of brainCatalog) {
      const value = `catalog:${brain.path}`;
      const label = [
        `${brain.name} (${formatBrainArchitectureLabel(brain.brainArchitectureKind)})`,
        samePath(brain.path, species?.defaultBrainPath) ? "species default" : null
      ].filter(Boolean).join(" - ");
      pieces.push([
        `<option value="${escapeHtml(value)}"`,
        selected === value ? " selected" : "",
        brain.isCompatible === false ? " disabled" : "",
        ` title="${escapeHtml(`${brain.path} | ${formatBrainProfileTopology(brain)} | ${brainCompatibilityStatus(brain)}`)}">`,
        `${escapeHtml(label)}${brain.isCompatible === false ? " [incompatible]" : ""}`,
        `</option>`
      ].join(""));
    }

    if (missingBrainPath && !findBrainCatalogEntryByPath(missingBrainPath)) {
      const value = `catalog:${missingBrainPath}`;
      pieces.push(`<option value="${escapeHtml(value)}"${selected === value ? " selected" : ""}>Missing catalog brain: ${escapeHtml(missingBrainPath)}</option>`);
    }
  }

  return pieces.join("");
}

function defaultSpeciesBrainChoiceValue(species = selectedSpeciesCatalogEntry()) {
  return species?.defaultBrainPath
    ? `catalog:${species.defaultBrainPath}`
    : "profile";
}

function normalizeBrainChoiceValue(value) {
  if (value === "generated:scenario") {
    const scenarioDefaultKind = scenarioEditor?.scenario?.initialBrainKind;
    return scenarioDefaultKind ? `generated:${scenarioDefaultKind}` : "profile";
  }

  return value;
}

function scenarioBrainArchitectureKind() {
  return scenarioEditor?.scenario?.brainArchitectureKind || "hybridNeural";
}

function sameEnumValue(left, right) {
  return String(left ?? "").toLowerCase() === String(right ?? "").toLowerCase();
}

function samePath(left, right) {
  return String(left ?? "").replace(/\\/g, "/").toLowerCase() === String(right ?? "").replace(/\\/g, "/").toLowerCase();
}

function formatBrainArchitectureLabel(architecture) {
  return sameEnumValue(architecture, "rtNeatGraph")
    ? "rtNEAT graph"
    : formatEnumLabel(architecture || "scenario architecture");
}

function brainArchitectureIsRtNeat(architecture) {
  return sameEnumValue(architecture, "rtNeatGraph");
}

function formatBrainProfileTopology(brain) {
  if (!brain) {
    return "unknown topology";
  }

  const weights = `${formatNumber(brain.weightCount)} weights`;
  return brainArchitectureIsRtNeat(brain.brainArchitectureKind)
    ? `graph topology, hidden ${formatNumber(brain.hiddenNodeCount)}, ${weights}`
    : `hidden ${formatNumber(brain.hiddenNodeCount)}, ${weights}`;
}

function selectedBrainCatalogEntry() {
  return findBrainCatalogEntryByPath(brainCatalogSelect?.value);
}

function findBrainCatalogEntryByPath(path) {
  return brainCatalog.find((candidate) => samePath(candidate.path, path)) ?? null;
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

async function loadBrainLabWorldProbeFixtures(selectedPath = brainLabWorldProbeFixtureSelect?.value || "") {
  if (!brainLabWorldProbeFixtureSelect) {
    return;
  }

  const response = await fetch("/api/brain-lab/probe-fixtures");
  brainLabWorldProbeFixtures = response.ok ? await response.json() : [];
  renderBrainLabWorldProbeFixtureOptions(selectedPath);
  updateBrainLabButtons();
}

function renderBrainLabWorldProbeFixtureOptions(selectedPath = "") {
  if (!brainLabWorldProbeFixtureSelect) {
    return;
  }

  brainLabWorldProbeFixtureSelect.innerHTML = "";
  const empty = document.createElement("option");
  empty.value = "";
  empty.textContent = brainLabWorldProbeFixtures.length > 0 ? "Choose setup" : "No probe setups";
  brainLabWorldProbeFixtureSelect.append(empty);

  for (const fixture of brainLabWorldProbeFixtures) {
    const option = document.createElement("option");
    option.value = fixture.path;
    option.textContent = `${fixture.name}${fixture.isBuiltIn ? " (built-in)" : ""}`;
    option.title = fixture.description || fixture.path;
    brainLabWorldProbeFixtureSelect.append(option);
  }

  brainLabWorldProbeFixtureSelect.value = [...brainLabWorldProbeFixtureSelect.options].some((option) => option.value === selectedPath)
    ? selectedPath
    : "";
}

function selectedBrainLabWorldProbeFixture() {
  const path = brainLabWorldProbeFixtureSelect?.value || "";
  return brainLabWorldProbeFixtures.find((fixture) => fixture.path === path) ?? null;
}

function applyBrainLabWorldProbeEnvironmentProfile(options = {}) {
  const { evaluate = true } = options;
  const key = brainLabWorldProbeEnvironmentSelect?.value || "snapshot";
  const profile = brainLabWorldProbeEnvironmentProfiles[key];
  if (!profile) {
    return;
  }

  setBrainLabWorldProbeEnvironmentControls(profile);
  clearBrainLabPopulation();
  renderBrainLabWorldProbe();
  updateBrainLabButtons();
  if (evaluate && brainLabWorldProbeScene && brainLabEvaluation) {
    scheduleBrainLabEvaluate(80);
  }
}

function markBrainLabWorldProbeEnvironmentCustom() {
  if (brainLabWorldProbeEnvironmentSelect) {
    brainLabWorldProbeEnvironmentSelect.value = "custom";
  }

  clearBrainLabPopulation();
  renderBrainLabWorldProbe();
  updateBrainLabButtons();
  if (brainLabWorldProbeScene && brainLabEvaluation) {
    scheduleBrainLabEvaluate(80);
  }
}

function markBrainLabWorldProbeBoundaryCustom() {
  if (brainLabWorldProbeBoundarySelect?.value !== "none" && brainLabWorldProbeBiomeSelect?.value === "snapshot") {
    brainLabWorldProbeBiomeSelect.value = brainLabWorldProbeDefaultBoundaryBiome();
  }
  markBrainLabWorldProbeEnvironmentCustom();
}

function brainLabWorldProbeDefaultBoundaryBiome() {
  return brainLabWorldProbeScene?.environment?.currentBiomeKind === "Desert" ? "Forest" : "Desert";
}

function resetBrainLabWorldProbeEnvironment() {
  if (brainLabWorldProbeEnvironmentSelect) {
    brainLabWorldProbeEnvironmentSelect.value = "snapshot";
  }
  setBrainLabWorldProbeEnvironmentControls(brainLabWorldProbeEnvironmentProfiles.snapshot);
}

function setBrainLabWorldProbeEnvironmentControls(profile) {
  if (brainLabWorldProbeBiomeSelect) {
    brainLabWorldProbeBiomeSelect.value = profile.biomeKind || "snapshot";
  }
  if (brainLabWorldProbeBoundarySelect) {
    brainLabWorldProbeBoundarySelect.value = profile.boundaryDirection || "none";
  }
  if (brainLabWorldProbeBoundaryOffsetInput) {
    brainLabWorldProbeBoundaryOffsetInput.value = formatBrainLabControlValue(profile.boundaryOffset || 0);
  }
  if (brainLabWorldProbeFertilityInput) {
    brainLabWorldProbeFertilityInput.value = profile.localFertility === "" || profile.localFertility == null
      ? ""
      : formatBrainLabControlValue(profile.localFertility);
  }
  if (brainLabWorldProbeObstacleSelect) {
    brainLabWorldProbeObstacleSelect.value = profile.obstacleMode || "snapshot";
  }
}

function buildBrainLabWorldProbeEnvironmentPayload() {
  const biomeKind = brainLabWorldProbeBiomeSelect?.value || "snapshot";
  const boundaryDirection = brainLabWorldProbeBoundarySelect?.value || "none";
  const boundaryOffset = Number(brainLabWorldProbeBoundaryOffsetInput?.value || 0);
  const obstacleMode = brainLabWorldProbeObstacleSelect?.value || "snapshot";
  const fertilityText = (brainLabWorldProbeFertilityInput?.value || "").trim();
  const payload = {};

  if (boundaryDirection !== "none" && biomeKind && biomeKind !== "snapshot") {
    payload.biomeBoundary = {
      direction: boundaryDirection,
      nearBiomeKind: "snapshot",
      farBiomeKind: biomeKind,
      offset: Number.isFinite(boundaryOffset) ? boundaryOffset : 0
    };
  } else if (biomeKind && biomeKind !== "snapshot") {
    payload.biomeKind = biomeKind;
  }
  if (fertilityText) {
    const fertility = Number(fertilityText);
    if (Number.isFinite(fertility)) {
      payload.localFertility = Math.min(Math.max(fertility, 0.05), 1);
    }
  }
  if (obstacleMode && obstacleMode !== "snapshot") {
    payload.obstacleMode = obstacleMode;
  }

  return Object.keys(payload).length > 0 ? payload : null;
}

function isBrainLabWorldProbeEnvironmentActive() {
  return Boolean(buildBrainLabWorldProbeEnvironmentPayload());
}

function brainLabWorldProbeEnvironmentSummary() {
  const environment = buildBrainLabWorldProbeEnvironmentPayload();
  if (!environment) {
    return "snapshot";
  }

  return [
    environment.biomeBoundary ? `boundary ${brainLabWorldProbeBoundaryLabel(environment.biomeBoundary.direction)} ${environment.biomeBoundary.farBiomeKind} offset ${formatBrainLabNumber(environment.biomeBoundary.offset || 0)}u` : null,
    environment.biomeKind ? `biome ${environment.biomeKind}` : null,
    environment.localFertility != null ? `fertility ${formatBrainLabNumber(environment.localFertility)}` : null,
    environment.obstacleMode ? `obstacles ${environment.obstacleMode}` : null
  ].filter(Boolean).join(", ");
}

function brainLabWorldProbeBoundaryLabel(direction) {
  return {
    forward: "ahead",
    behind: "behind",
    left: "left",
    right: "right"
  }[direction] || "ahead";
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
  brainLabPopulationEvaluation = null;
  brainLabPresetMatrixResult = null;
  brainLabProbeTestResult = null;
  brainLabProfileComparisonResult = null;
  brainLabProfileComparisonCohortKey = null;
  brainLabSelectedInputKey = null;
  brainLabWorldProbeScene = null;
  brainLabWorldProbeBaseScene = null;
  brainLabOverrides = {};
  brainLabWorldProbeOverrideKeys = new Set();
  resetBrainLabWorldProbeEdits();
  resetBrainLabWorldProbeZoom(false);
  resetBrainLabWorldProbeToggles();
  resetBrainLabWorldProbeEnvironment();
  if (brainLabSnapshotPath) {
    brainLabSnapshotPath.value = brainLabSnapshot.path;
  }
  if (brainLabSnapshotSelect && [...brainLabSnapshotSelect.options].some((option) => option.value === brainLabSnapshot.path)) {
    brainLabSnapshotSelect.value = brainLabSnapshot.path;
  }

  renderBrainLabSnapshot();
  brainLabStatus.textContent = `Loaded ${brainLabSnapshot.path}`;
  if (brainLabSnapshot.creatures?.length > 0) {
    await loadBrainLabWorldProbe();
    await evaluateBrainLab();
  }
}

function renderBrainLabSnapshot() {
  renderBrainLabMeta();
  renderBrainLabCreatureOptions();
  renderBrainLabInputs();
  renderBrainLabOutputs();
  renderBrainLabProbeTests();
  renderBrainLabPopulation();
  renderBrainLabPresetMatrix();
  renderBrainLabProfileComparison();
  renderBrainLabWorldProbe();
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

function selectedBrainLabCreature() {
  const creatureId = Number(brainLabCreatureSelect?.value || 0);
  if (!creatureId) {
    return null;
  }

  return (brainLabSnapshot?.creatures || []).find((creature) => Number(creature.id) === creatureId) ?? null;
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
  const worldProbeEnvironment = buildBrainLabWorldProbeEnvironmentPayload();
  const response = await fetch("/api/brain-lab/evaluate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      creatureId,
      inputOverrides: brainLabOverrides,
      worldProbe: buildBrainLabWorldProbeEditPayload(Boolean(worldProbeEnvironment)),
      worldProbeEnvironment
    })
  });

  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Evaluation failed.");
    return;
  }

  brainLabEvaluation = await response.json();
  if (brainLabSelectedInputKey
    && !(brainLabEvaluation.inputs || []).some((input) => input.key === brainLabSelectedInputKey)) {
    brainLabSelectedInputKey = null;
  }
  renderBrainLabMeta();
  renderBrainLabInputs();
  renderBrainLabOutputs();
  renderBrainLabPopulation();
  renderBrainLabWorldProbe();
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

async function loadBrainLabWorldProbe() {
  const path = brainLabSelectedPath();
  const creatureId = Number(brainLabCreatureSelect?.value || 0);
  if (!path || !creatureId) {
    brainLabWorldProbeScene = null;
    brainLabWorldProbeBaseScene = null;
    renderBrainLabWorldProbe();
    return;
  }

  const response = await fetch("/api/brain-lab/world-probe", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      creatureId
    })
  });

  if (!response.ok) {
    brainLabWorldProbeScene = null;
    brainLabWorldProbeBaseScene = null;
    renderBrainLabWorldProbe();
    brainLabStatus.textContent = await responseErrorMessage(response, "World probe failed.");
    return;
  }

  brainLabWorldProbeBaseScene = await response.json();
  brainLabWorldProbeScene = cloneBrainLabWorldProbeScene(brainLabWorldProbeBaseScene);
  resetBrainLabWorldProbeEdits({ restoreScene: false, clearSelection: true });
  renderBrainLabWorldProbe();
}

function cloneBrainLabWorldProbeScene(scene) {
  return scene ? JSON.parse(JSON.stringify(scene)) : null;
}

function renderBrainLabWorldProbe() {
  if (!brainLabWorldProbeCanvas || !brainLabWorldProbeSummary) {
    return;
  }

  brainLabWorldProbeHitTargets = [];
  const canvas = brainLabWorldProbeCanvas;
  resizeBrainLabWorldProbeCanvas(canvas);
  const context = canvas.getContext("2d");
  const width = canvas.width;
  const height = canvas.height;
  context.clearRect(0, 0, width, height);
  context.fillStyle = "#f5f8fb";
  context.fillRect(0, 0, width, height);

  if (!brainLabWorldProbeScene) {
    brainLabWorldProbeSelected = null;
    brainLabWorldProbeSummary.textContent = "No world probe yet.";
    renderBrainLabWorldProbeSelection();
    renderBrainLabWorldProbeTrace();
    updateBrainLabWorldProbeZoomControls();
    return;
  }

  const toggles = brainLabWorldProbeToggles();
  const scene = brainLabWorldProbeScene;
  const transform = brainLabWorldProbeTransform(canvas);
  const { centerX, centerY, radius, scale } = transform;
  const toScreen = (item) => brainLabWorldProbeToScreen(item, transform);
  const visionTrace = brainLabWorldProbeVisionTrace(scene);

  drawBrainLabProbeCircle(context, centerX, centerY, Number(scene.soundRadius || 0) * scale, "#e7a13d", [8, 8]);
  drawBrainLabProbeCircle(context, centerX, centerY, Number(scene.senseRadius || 0) * scale, "#5078bd", [4, 7]);
  drawBrainLabProbeCircle(context, centerX, centerY, radius * scale, "#9aa5b1", []);
  drawBrainLabProbeVisionCone(context, scene, transform, visionTrace);
  drawBrainLabWorldProbeBiomeCues(context, scene, transform);

  if (toggles.sound && toggles.creatures) {
    for (const creature of scene.creatures || []) {
      if (isBrainLabWorldProbeCreatureVisible(creature) && isBrainLabWorldProbeSoundVisible(creature)) {
        const point = toScreen(creature);
        drawBrainLabProbeSound(context, point, creature);
        addBrainLabWorldProbeHitTarget({
          type: "sound",
          id: creature.id,
          key: brainLabWorldProbeSoundKey(creature.id),
          item: creature,
          point,
          radius: 8 + Math.max(0, Number(creature.soundAmplitude || 0)) * 22,
          priority: 1
        });
      }
    }
  }

  if (toggles.plants) {
    for (const resource of (scene.resources || []).filter((item) => item.kind === "Plant")) {
      if (!isBrainLabWorldProbeResourceVisible(resource)) {
        continue;
      }

      const point = toScreen(resource);
      const radius = Math.max(3, Number(resource.radius || 0) * scale);
      drawBrainLabProbeDot(context, point, radius, "#3d9462", "#1f6d43");
      drawBrainLabProbeTraceHighlight(context, point, radius, brainLabWorldProbeTraceContribution(visionTrace, "resource", resource.id));
      addBrainLabWorldProbeHitTarget({
        type: "resource",
        id: resource.id,
        key: brainLabWorldProbeObjectKey("resource", resource.id),
        item: resource,
        point,
        radius: Math.min(12, radius),
        priority: 2
      });
    }
  }

  if (toggles.meatEggs) {
    for (const resource of (scene.resources || []).filter((item) => item.kind === "Meat")) {
      if (!isBrainLabWorldProbeResourceVisible(resource)) {
        continue;
      }

      const point = toScreen(resource);
      const radius = Math.max(3, Number(resource.radius || 0) * scale);
      drawBrainLabProbeDot(context, point, radius, "#b95c52", "#873b34");
      drawBrainLabProbeTraceHighlight(context, point, radius, brainLabWorldProbeTraceContribution(visionTrace, "resource", resource.id));
      addBrainLabWorldProbeHitTarget({
        type: "resource",
        id: resource.id,
        key: brainLabWorldProbeObjectKey("resource", resource.id),
        item: resource,
        point,
        radius: Math.min(12, radius),
        priority: 2
      });
    }

    for (const egg of scene.eggs || []) {
      if (!isBrainLabWorldProbeEggVisible(egg)) {
        continue;
      }

      const point = toScreen(egg);
      const radius = Math.max(3, Number(egg.radius || 0) * scale);
      drawBrainLabProbeDot(context, point, radius, "#d7ad34", "#9b7b1e");
      drawBrainLabProbeTraceHighlight(context, point, radius, brainLabWorldProbeTraceContribution(visionTrace, "egg", egg.id));
      addBrainLabWorldProbeHitTarget({
        type: "egg",
        id: egg.id,
        key: brainLabWorldProbeObjectKey("egg", egg.id),
        item: egg,
        point,
        radius: Math.min(12, radius),
        priority: 2
      });
    }
  }

  if (toggles.smallPrey) {
    for (const prey of scene.smallPrey || []) {
      if (!isBrainLabWorldProbeSmallPreyVisible(prey)) {
        continue;
      }

      const point = toScreen(prey);
      const preyRadius = drawBrainLabProbeSmallPrey(context, point, prey, scale);
      drawBrainLabProbeTraceHighlight(context, point, preyRadius, brainLabWorldProbeTraceContribution(visionTrace, "smallPrey", prey.id));
      addBrainLabWorldProbeHitTarget({
        type: "smallPrey",
        id: prey.id,
        key: brainLabWorldProbeObjectKey("smallPrey", prey.id),
        item: prey,
        point,
        radius: preyRadius,
        priority: 2.5
      });
    }
  }

  if (toggles.creatures) {
    for (const creature of scene.creatures || []) {
      if (!isBrainLabWorldProbeCreatureVisible(creature)) {
        continue;
      }

      const point = toScreen(creature);
      const bodyRadius = drawBrainLabProbeCreature(context, point, creature, scale);
      drawBrainLabProbeTraceHighlight(context, point, bodyRadius, brainLabWorldProbeTraceContribution(visionTrace, "creature", creature.id));
      addBrainLabWorldProbeHitTarget({
        type: "creature",
        id: creature.id,
        key: brainLabWorldProbeObjectKey("creature", creature.id),
        item: creature,
        point,
        radius: bodyRadius,
        priority: 3
      });
    }
  }

  const focusPoint = toScreen(scene.focus);
  const focusRadius = drawBrainLabProbeCreature(context, focusPoint, scene.focus, scale);
  addBrainLabWorldProbeHitTarget({
    type: "focus",
    id: scene.focus.id,
    key: brainLabWorldProbeObjectKey("focus", scene.focus.id),
    item: scene.focus,
    point: focusPoint,
    radius: focusRadius,
    priority: 4
  });
  drawBrainLabWorldProbeSelectedTarget(context);

  renderBrainLabWorldProbeSummary(scene, toggles);
  renderBrainLabWorldProbeSelection();
  renderBrainLabWorldProbeTrace(visionTrace);
  updateBrainLabWorldProbeZoomControls();
}

function renderBrainLabWorldProbeSummary(scene, toggles) {
  const counts = brainLabWorldProbeVisibleSummary(scene, toggles);
  const activeSamples = brainLabWorldProbeActiveBiomeSamples(scene);
  const hasActiveEnvironment = isBrainLabWorldProbeEnvironmentActive();
  brainLabWorldProbeSummary.innerHTML = [
    brainLabWorldProbeSummaryCell("Environment", brainLabWorldProbeEnvironmentSummary()),
    activeSamples.currentBiomeKind ? brainLabWorldProbeSummaryCell("Probe biome", formatEnumLabel(activeSamples.currentBiomeKind)) : "",
    activeSamples.currentBiomeKind ? brainLabWorldProbeSummaryCell("Probe samples", brainLabWorldProbeBiomeSampleSummary(activeSamples)) : "",
    brainLabWorldProbeSummaryCell("Vision cone", brainLabWorldProbeVisionConeSummary(scene)),
    hasActiveEnvironment && scene.environment?.currentBiomeKind
      ? brainLabWorldProbeSummaryCell("Snapshot biome", formatEnumLabel(scene.environment.currentBiomeKind))
      : "",
    scene.environment?.localFertility != null ? brainLabWorldProbeSummaryCell("Snapshot fertility", formatBrainLabNumber(scene.environment.localFertility)) : "",
    scene.environment ? brainLabWorldProbeSummaryCell("Snapshot obstacles", brainLabWorldProbeObstacleSummary(scene.environment)) : "",
    brainLabWorldProbeSummaryCell("Probe radius", `${formatBrainLabNumber(scene.probeRadius)}u`),
    brainLabWorldProbeSummaryCell("Sense radius", `${formatBrainLabNumber(scene.senseRadius)}u`),
    brainLabWorldProbeSummaryCell("Sound radius", `${formatBrainLabNumber(scene.soundRadius)}u`),
    brainLabWorldProbeSummaryCell("Plants shown", `${formatNumber(counts.plants.visible)} / ${formatNumber(counts.plants.total)}`),
    brainLabWorldProbeSummaryCell("Meat shown", `${formatNumber(counts.meat.visible)} / ${formatNumber(counts.meat.total)}`),
    brainLabWorldProbeSummaryCell("Eggs shown", `${formatNumber(counts.eggs.visible)} / ${formatNumber(counts.eggs.total)}`),
    brainLabWorldProbeSummaryCell("Small prey shown", `${formatNumber(counts.smallPrey.visible)} / ${formatNumber(counts.smallPrey.total)}`),
    brainLabWorldProbeSummaryCell("Creatures shown", `${formatNumber(counts.creatures.visible)} / ${formatNumber(counts.creatures.total)}`),
    brainLabWorldProbeSummaryCell("Sound shown", `${formatNumber(counts.sound.visible)} / ${formatNumber(counts.sound.total)}`),
    brainLabWorldProbeEdited ? brainLabWorldProbeSummaryCell("Probe edits", "active") : "",
    brainLabWorldProbeHiddenKeys.size > 0 ? brainLabWorldProbeSummaryCell("Hidden edits", formatNumber(brainLabWorldProbeHiddenKeys.size)) : "",
    brainLabWorldProbeMutedSoundKeys.size > 0 ? brainLabWorldProbeSummaryCell("Muted edits", formatNumber(brainLabWorldProbeMutedSoundKeys.size)) : "",
    scene.truncated ? brainLabWorldProbeSummaryCell("Probe data", "view capped") : ""
  ].filter(Boolean).join("");
}

function brainLabWorldProbeBoundarySummary(environment) {
  if (!environment?.currentBiomeKind) {
    return "";
  }

  const current = environment.currentBiomeKind;
  const parts = [
    environment.forwardBiomeKind && environment.forwardBiomeKind !== current
      ? `forward ${formatEnumLabel(environment.forwardBiomeKind)}`
      : null,
    environment.leftBiomeKind && environment.leftBiomeKind !== current
      ? `left ${formatEnumLabel(environment.leftBiomeKind)}`
      : null,
    environment.rightBiomeKind && environment.rightBiomeKind !== current
      ? `right ${formatEnumLabel(environment.rightBiomeKind)}`
      : null
  ].filter(Boolean);
  return parts.join(", ");
}

function brainLabWorldProbeActiveBiomeSamples(scene) {
  const snapshot = scene?.environment || {};
  const environment = buildBrainLabWorldProbeEnvironmentPayload();
  if (environment?.biomeBoundary && scene?.focus) {
    const boundary = environment.biomeBoundary;
    const nearBiome = boundary.nearBiomeKind && boundary.nearBiomeKind !== "snapshot"
      ? boundary.nearBiomeKind
      : snapshot.currentBiomeKind;
    const farBiome = boundary.farBiomeKind || snapshot.currentBiomeKind;
    const focus = {
      x: Number(scene.focus.x || 0),
      y: Number(scene.focus.y || 0)
    };
    const heading = Number(scene.focus.headingRadians || 0);
    const axis = brainLabWorldProbeWorldAxis(heading, boundary.direction || "forward");
    const origin = {
      x: focus.x + axis.x * Number(boundary.offset || 0),
      y: focus.y + axis.y * Number(boundary.offset || 0)
    };
    const sample = (point) => {
      const projection = (point.x - origin.x) * axis.x + (point.y - origin.y) * axis.y;
      return projection >= 0 ? farBiome : nearBiome;
    };
    const distance = brainLabWorldProbeHabitatProbeDistance(scene);
    const forward = brainLabWorldProbeWorldAxis(heading, "forward");
    const left = brainLabWorldProbeWorldAxis(heading, "left");
    const right = brainLabWorldProbeWorldAxis(heading, "right");
    return {
      currentBiomeKind: sample(focus),
      forwardBiomeKind: sample({ x: focus.x + forward.x * distance, y: focus.y + forward.y * distance }),
      leftBiomeKind: sample({ x: focus.x + left.x * distance, y: focus.y + left.y * distance }),
      rightBiomeKind: sample({ x: focus.x + right.x * distance, y: focus.y + right.y * distance })
    };
  }

  if (environment?.biomeKind) {
    return {
      currentBiomeKind: environment.biomeKind,
      forwardBiomeKind: environment.biomeKind,
      leftBiomeKind: environment.biomeKind,
      rightBiomeKind: environment.biomeKind
    };
  }

  return {
    currentBiomeKind: snapshot.currentBiomeKind,
    forwardBiomeKind: snapshot.forwardBiomeKind,
    leftBiomeKind: snapshot.leftBiomeKind,
    rightBiomeKind: snapshot.rightBiomeKind
  };
}

function brainLabWorldProbeWorldAxis(headingRadians, direction) {
  const heading = Number(headingRadians || 0);
  const forward = { x: Math.cos(heading), y: Math.sin(heading) };
  const right = { x: -Math.sin(heading), y: Math.cos(heading) };
  if (direction === "behind") {
    return { x: -forward.x, y: -forward.y };
  }
  if (direction === "left") {
    return { x: -right.x, y: -right.y };
  }
  if (direction === "right") {
    return right;
  }
  return forward;
}

function brainLabWorldProbeBiomeSampleSummary(samples) {
  if (!samples?.currentBiomeKind) {
    return "unknown";
  }

  return [
    `current ${formatEnumLabel(samples.currentBiomeKind)}`,
    `F ${formatEnumLabel(samples.forwardBiomeKind || samples.currentBiomeKind)}`,
    `L ${formatEnumLabel(samples.leftBiomeKind || samples.currentBiomeKind)}`,
    `R ${formatEnumLabel(samples.rightBiomeKind || samples.currentBiomeKind)}`
  ].join(", ");
}

function brainLabWorldProbeObstacleSummary(environment) {
  if (environment.currentObstacleBlocked) {
    return "focus blocked";
  }
  return Number(environment.obstacleBlockedCellCount || 0) > 0
    ? `${formatNumber(environment.obstacleBlockedCellCount)} blocked cells`
    : "none";
}

function brainLabWorldProbeVisibleSummary(scene, toggles) {
  const plants = (scene.resources || []).filter((resource) => resource.kind === "Plant");
  const meat = (scene.resources || []).filter((resource) => resource.kind === "Meat");
  const eggs = scene.eggs || [];
  const smallPrey = scene.smallPrey || [];
  const creatures = scene.creatures || [];
  const sound = creatures.filter((creature) => Number(creature.soundAmplitude || 0) > 0.05);
  return {
    plants: {
      total: plants.length,
      visible: toggles.plants ? plants.filter(isBrainLabWorldProbeResourceVisible).length : 0
    },
    meat: {
      total: meat.length,
      visible: toggles.meatEggs ? meat.filter(isBrainLabWorldProbeResourceVisible).length : 0
    },
    eggs: {
      total: eggs.length,
      visible: toggles.meatEggs ? eggs.filter(isBrainLabWorldProbeEggVisible).length : 0
    },
    smallPrey: {
      total: smallPrey.length,
      visible: toggles.smallPrey ? smallPrey.filter(isBrainLabWorldProbeSmallPreyVisible).length : 0
    },
    creatures: {
      total: creatures.length,
      visible: toggles.creatures ? creatures.filter(isBrainLabWorldProbeCreatureVisible).length : 0
    },
    sound: {
      total: sound.length,
      visible: toggles.creatures && toggles.sound
        ? sound.filter((creature) => isBrainLabWorldProbeCreatureVisible(creature) && isBrainLabWorldProbeSoundVisible(creature)).length
        : 0
    }
  };
}

function brainLabWorldProbeSummaryCell(label, value) {
  return `<span><strong>${escapeHtml(label)}</strong> ${escapeHtml(value)}</span>`;
}

function resizeBrainLabWorldProbeCanvas(canvas) {
  const width = Math.max(320, Math.round(canvas.clientWidth || canvas.width));
  const height = Math.max(220, Math.round(canvas.clientHeight || canvas.height));
  if (canvas.width !== width) {
    canvas.width = width;
  }
  if (canvas.height !== height) {
    canvas.height = height;
  }
}

function brainLabWorldProbeTransform(canvas = brainLabWorldProbeCanvas) {
  const width = canvas?.width || 1;
  const height = canvas?.height || 1;
  const scene = brainLabWorldProbeScene || {};
  const centerX = width / 2 + brainLabWorldProbePanX;
  const centerY = height / 2 + brainLabWorldProbePanY;
  const radius = Math.max(1, Number(scene.probeRadius || 1));
  const scale = Math.min((width - 34) / (radius * 2), (height - 34) / (radius * 2)) * brainLabWorldProbeZoom;
  return { width, height, centerX, centerY, radius, scale };
}

function brainLabWorldProbeToScreen(item, transform = brainLabWorldProbeTransform()) {
  return {
    x: transform.centerX + Number(item?.x || 0) * transform.scale,
    y: transform.centerY - Number(item?.y || 0) * transform.scale
  };
}

function brainLabWorldProbeWorldFromCanvasPoint(point) {
  if (!point || !brainLabWorldProbeScene) {
    return null;
  }

  const transform = brainLabWorldProbeTransform();
  return {
    x: (point.x - transform.centerX) / transform.scale,
    y: (transform.centerY - point.y) / transform.scale
  };
}

function drawBrainLabProbeCircle(context, x, y, radius, color, dash) {
  if (!Number.isFinite(radius) || radius <= 0) {
    return;
  }

  context.save();
  context.setLineDash(dash);
  context.strokeStyle = color;
  context.lineWidth = 1.5;
  context.beginPath();
  context.arc(x, y, radius, 0, Math.PI * 2);
  context.stroke();
  context.restore();
}

function drawBrainLabProbeVisionCone(context, scene, transform, trace = null) {
  const focus = scene?.focus;
  if (!focus) {
    return;
  }

  const range = Number(scene.senseRadius || 0) * transform.scale;
  if (!Number.isFinite(range) || range <= 0) {
    return;
  }

  const point = brainLabWorldProbeToScreen(focus, transform);
  const heading = Number(focus.headingRadians || 0);
  const rawAngle = Number(focus.visionAngleRadians || Math.PI * 2);
  const angle = Math.max(0, Math.min(Math.PI * 2, rawAngle));
  if (!Number.isFinite(angle) || angle <= 0) {
    return;
  }

  const screenHeading = -heading;
  const halfAngle = angle * 0.5;
  context.save();
  context.fillStyle = "rgba(45, 119, 190, 0.075)";
  context.strokeStyle = "rgba(45, 119, 190, 0.52)";
  context.lineWidth = 1.5;

  if (angle >= Math.PI * 2 - 0.001) {
    context.beginPath();
    context.arc(point.x, point.y, range, 0, Math.PI * 2);
    context.fill();
    context.setLineDash([4, 6]);
    context.stroke();
    drawBrainLabProbeVisionSectorGuides(context, point, range, screenHeading, halfAngle, trace);
    context.restore();
    return;
  }

  const start = screenHeading - halfAngle;
  const end = screenHeading + halfAngle;
  const startPoint = {
    x: point.x + Math.cos(start) * range,
    y: point.y + Math.sin(start) * range
  };
  const endPoint = {
    x: point.x + Math.cos(end) * range,
    y: point.y + Math.sin(end) * range
  };

  context.beginPath();
  context.moveTo(point.x, point.y);
  context.lineTo(startPoint.x, startPoint.y);
  context.arc(point.x, point.y, range, start, end);
  context.lineTo(point.x, point.y);
  context.closePath();
  context.fill();
  context.stroke();

  drawBrainLabProbeVisionSectorGuides(context, point, range, screenHeading, halfAngle, trace);
  context.strokeStyle = "rgba(45, 119, 190, 0.35)";
  context.setLineDash([5, 6]);
  context.beginPath();
  context.moveTo(point.x, point.y);
  context.lineTo(point.x + Math.cos(screenHeading) * range, point.y + Math.sin(screenHeading) * range);
  context.stroke();
  context.restore();
}

function drawBrainLabProbeVisionSectorGuides(context, point, range, screenHeading, halfAngle, trace = null) {
  const sectorCount = 9;
  const selectedSector = Number.isInteger(trace?.spec?.sectorIndex) ? trace.spec.sectorIndex : null;

  if (selectedSector !== null) {
    const start = brainLabWorldProbeSectorScreenAngle(screenHeading, halfAngle, selectedSector + 1, sectorCount);
    const end = brainLabWorldProbeSectorScreenAngle(screenHeading, halfAngle, selectedSector, sectorCount);
    context.save();
    context.fillStyle = "rgba(39, 105, 210, 0.13)";
    context.strokeStyle = "rgba(39, 105, 210, 0.45)";
    context.lineWidth = 1.5;
    context.beginPath();
    context.moveTo(point.x, point.y);
    context.lineTo(point.x + Math.cos(start) * range, point.y + Math.sin(start) * range);
    context.arc(point.x, point.y, range, start, end);
    context.lineTo(point.x, point.y);
    context.closePath();
    context.fill();
    context.stroke();
    context.restore();
  }

  context.save();
  context.strokeStyle = "rgba(45, 119, 190, 0.22)";
  context.lineWidth = 1;
  context.setLineDash([3, 7]);
  for (let index = 1; index < sectorCount; index += 1) {
    const angle = brainLabWorldProbeSectorScreenAngle(screenHeading, halfAngle, index, sectorCount);
    context.beginPath();
    context.moveTo(point.x, point.y);
    context.lineTo(point.x + Math.cos(angle) * range, point.y + Math.sin(angle) * range);
    context.stroke();
  }
  context.restore();
}

function brainLabWorldProbeSectorScreenAngle(screenHeading, halfAngle, sectorBoundaryIndex, sectorCount) {
  return screenHeading + halfAngle - (halfAngle * 2) * sectorBoundaryIndex / sectorCount;
}

function drawBrainLabProbeTraceHighlight(context, point, radius, contribution) {
  if (!contribution) {
    return;
  }

  context.save();
  context.strokeStyle = contribution.primary ? "#1f57d6" : "rgba(31, 87, 214, 0.78)";
  context.lineWidth = contribution.primary ? 3 : 2;
  context.setLineDash(contribution.primary ? [] : [4, 4]);
  context.beginPath();
  context.arc(point.x, point.y, Math.max(8, radius + 6), 0, Math.PI * 2);
  context.stroke();
  context.restore();
}

function brainLabWorldProbeVisionConeSummary(scene) {
  const radians = Number(scene?.focus?.visionAngleRadians || 0);
  if (!Number.isFinite(radians) || radians <= 0) {
    return "unknown";
  }

  const degrees = radians * 180 / Math.PI;
  return `${formatBrainLabNumber(degrees)} deg`;
}

function selectedBrainLabInput() {
  if (!brainLabSelectedInputKey) {
    return null;
  }

  return (brainLabEvaluation?.inputs || []).find((input) => input.key === brainLabSelectedInputKey) ?? null;
}

function selectBrainLabInput(key) {
  if (!key) {
    return;
  }

  brainLabSelectedInputKey = key;
  renderBrainLabInputs();
  renderBrainLabWorldProbe();
}

function brainLabWorldProbeVisionTrace(scene, input = selectedBrainLabInput()) {
  const spec = brainLabVisionTraceSpec(input?.key || "");
  if (!scene || !input || !spec) {
    return null;
  }

  const contributors = brainLabWorldProbeVisionContributors(scene, spec)
    .sort((left, right) => Number(left.distance || 0) - Number(right.distance || 0));
  const primary = brainLabVisionTraceUsesNearest(spec.signal) ? contributors[0] : null;
  const contributionByKey = new Map();
  for (const contribution of contributors) {
    contribution.primary = primary ? contribution.key === primary.key : false;
    contributionByKey.set(contribution.key, contribution);
  }

  return {
    input,
    spec,
    contributors,
    contributionByKey,
    nearest: contributors[0] ?? null,
    densityEstimate: Math.min(1, contributors.length / 8)
  };
}

function brainLabVisionTraceSpec(key) {
  if (!key || !key.startsWith("vision.")) {
    return null;
  }

  const sectorMatch = key.match(/^vision\.sector\.(\d+)\.([a-z0-9_]+)$/);
  const sectorIndex = sectorMatch ? Number(sectorMatch[1]) : null;
  const signal = sectorMatch ? sectorMatch[2] : key.slice("vision.".length);
  const categories = brainLabVisionTraceCategories(signal);
  if (categories.length === 0) {
    return null;
  }

  return {
    key,
    signal,
    sectorIndex: Number.isInteger(sectorIndex) && sectorIndex >= 0 && sectorIndex < 9 ? sectorIndex : null,
    categories
  };
}

function brainLabVisionTraceCategories(signal) {
  if (!signal) {
    return [];
  }

  if (signal.includes("plant")) {
    return ["plant"];
  }
  if (signal.includes("meat")) {
    return ["meat"];
  }
  if (signal.includes("egg")) {
    return ["egg"];
  }
  if (signal.includes("creature")) {
    return ["creature"];
  }
  if (signal.includes("food")) {
    return ["plant", "meat", "egg"];
  }
  return [];
}

function brainLabWorldProbeVisionContributors(scene, spec) {
  const contributors = [];
  const toggles = brainLabWorldProbeToggles();

  if (spec.categories.includes("plant") && toggles.plants) {
    for (const resource of (scene.resources || []).filter((item) => item.kind === "Plant")) {
      if (isBrainLabWorldProbeResourceVisible(resource)) {
        brainLabWorldProbeAddVisionContributor(contributors, scene, spec, "resource", "Plant", resource);
      }
    }
  }

  if (spec.categories.includes("meat") && toggles.meatEggs) {
    for (const resource of (scene.resources || []).filter((item) => item.kind === "Meat")) {
      if (isBrainLabWorldProbeResourceVisible(resource)) {
        brainLabWorldProbeAddVisionContributor(contributors, scene, spec, "resource", "Meat", resource);
      }
    }
  }

  if (spec.categories.includes("meat") && toggles.smallPrey) {
    for (const prey of scene.smallPrey || []) {
      if (isBrainLabWorldProbeSmallPreyVisible(prey)) {
        brainLabWorldProbeAddVisionContributor(contributors, scene, spec, "smallPrey", "Small prey", prey);
      }
    }
  }

  if (spec.categories.includes("egg") && toggles.meatEggs) {
    for (const egg of scene.eggs || []) {
      if (isBrainLabWorldProbeEggVisible(egg)) {
        brainLabWorldProbeAddVisionContributor(contributors, scene, spec, "egg", "Egg", egg);
      }
    }
  }

  if (spec.categories.includes("creature") && toggles.creatures) {
    for (const creature of scene.creatures || []) {
      if (isBrainLabWorldProbeCreatureVisible(creature) && brainLabWorldProbeCreatureMatchesTraceSize(scene, spec, creature)) {
        brainLabWorldProbeAddVisionContributor(contributors, scene, spec, "creature", "Creature", creature);
      }
    }
  }

  return contributors;
}

function brainLabWorldProbeAddVisionContributor(contributors, scene, spec, type, label, item) {
  const sample = brainLabWorldProbeVisionSample(scene, item);
  if (!sample.visible || (spec.sectorIndex !== null && sample.sectorIndex !== spec.sectorIndex)) {
    return;
  }

  contributors.push({
    type,
    id: item.id,
    key: brainLabWorldProbeTraceKey(type, item.id),
    label,
    item,
    distance: sample.distance,
    proximity: sample.proximity,
    sectorIndex: sample.sectorIndex,
    forward: sample.forward,
    right: sample.right,
    primary: false
  });
}

function brainLabWorldProbeVisionSample(scene, item) {
  const focus = scene?.focus || {};
  const x = Number(item?.x || 0);
  const y = Number(item?.y || 0);
  const distance = Math.hypot(x, y);
  const senseRadius = Math.max(0, Number(scene?.senseRadius || 0));
  const itemRadius = Math.max(0, Number(item?.radius || 0));
  const heading = Number(focus.headingRadians || 0);
  const visionAngle = Math.max(0, Math.min(Math.PI * 2, Number(focus.visionAngleRadians || Math.PI * 2)));
  const forward = x * Math.cos(heading) + y * Math.sin(heading);
  const right = x * -Math.sin(heading) + y * Math.cos(heading);
  const halfAngle = visionAngle >= Math.PI * 2 - 0.001
    ? Math.PI
    : Math.max(0.0001, Math.min(Math.PI, visionAngle * 0.5));
  const angle = Math.atan2(right, forward);
  const inRange = distance <= senseRadius + itemRadius;
  const inCone = distance <= 0.000001 || visionAngle >= Math.PI * 2 - 0.001 || (angle >= -halfAngle && angle <= halfAngle);
  const normalized = (angle + halfAngle) / (halfAngle * 2);
  const sectorIndex = Math.max(0, Math.min(8, Math.floor(normalized * 9)));
  return {
    visible: inRange && inCone,
    distance,
    proximity: senseRadius > 0 ? Math.max(0, Math.min(1, 1 - Math.max(0, distance - itemRadius) / senseRadius)) : 0,
    sectorIndex,
    forward: senseRadius > 0 ? Math.max(-1, Math.min(1, forward / senseRadius)) : 0,
    right: senseRadius > 0 ? Math.max(-1, Math.min(1, right / senseRadius)) : 0
  };
}

function brainLabWorldProbeCreatureMatchesTraceSize(scene, spec, creature) {
  if (!spec.signal.includes("smaller_creature")
    && !spec.signal.includes("similar_creature")
    && !spec.signal.includes("larger_creature")) {
    return true;
  }

  const focusRadius = Math.max(0.001, Number(scene?.focus?.radius || 1));
  const relative = (Number(creature?.radius || focusRadius) - focusRadius) / focusRadius;
  if (spec.signal.includes("smaller_creature")) {
    return relative < -0.2;
  }
  if (spec.signal.includes("larger_creature")) {
    return relative > 0.2;
  }
  return relative >= -0.2 && relative <= 0.2;
}

function brainLabVisionTraceUsesNearest(signal) {
  return signal.includes("proximity")
    || signal.includes("approach_rate")
    || signal.includes("facing_alignment");
}

function brainLabWorldProbeTraceContribution(trace, type, id) {
  return trace?.contributionByKey?.get(brainLabWorldProbeTraceKey(type, id)) ?? null;
}

function brainLabWorldProbeTraceKey(type, id) {
  return `${type}:${id}`;
}

function renderBrainLabWorldProbeTrace(trace = brainLabWorldProbeVisionTrace(brainLabWorldProbeScene)) {
  if (!brainLabWorldProbeTrace) {
    return;
  }

  const input = selectedBrainLabInput();
  if (!brainLabWorldProbeScene) {
    brainLabWorldProbeTrace.classList.add("empty");
    brainLabWorldProbeTrace.textContent = "No world probe yet.";
    return;
  }
  if (!input) {
    brainLabWorldProbeTrace.classList.add("empty");
    brainLabWorldProbeTrace.textContent = "Select a vision input to trace.";
    return;
  }
  if (!trace) {
    brainLabWorldProbeTrace.classList.add("empty");
    brainLabWorldProbeTrace.textContent = "No map trace for this input.";
    return;
  }

  const sectorText = trace.spec.sectorIndex === null ? "all sectors" : `sector ${trace.spec.sectorIndex}`;
  const nearestText = trace.nearest
    ? `${trace.nearest.label} #${formatNumber(trace.nearest.id)} ${formatBrainLabNumber(trace.nearest.distance)}u`
    : "none";
  const valueText = `${formatBrainLabNumber(input.baselineValue)} -> ${formatBrainLabNumber(input.modifiedValue)}`;
  const topContributors = trace.contributors.slice(0, 4).map((contribution) => `
    <div>
      <strong>${escapeHtml(contribution.label)} #${formatNumber(contribution.id)}</strong>
      ${formatBrainLabNumber(contribution.distance)}u
      prox ${formatBrainLabNumber(contribution.proximity)}
      F ${formatBrainLabNumber(contribution.forward)}
      R ${formatBrainLabNumber(contribution.right)}
    </div>
  `).join("");

  brainLabWorldProbeTrace.classList.remove("empty");
  brainLabWorldProbeTrace.innerHTML = `
    <div class="brain-lab-world-probe-trace-title">
      ${escapeHtml(input.name)}
      <code>${escapeHtml(input.key)}</code>
    </div>
    <div class="brain-lab-world-probe-trace-grid">
      <span><strong>Value</strong> ${escapeHtml(valueText)}</span>
      <span><strong>Scope</strong> ${escapeHtml(sectorText)}</span>
      <span><strong>Contributors</strong> ${formatNumber(trace.contributors.length)}</span>
      <span><strong>Nearest</strong> ${escapeHtml(nearestText)}</span>
      <span><strong>Density est.</strong> ${formatBrainLabNumber(trace.densityEstimate)}</span>
      <span><strong>Mode</strong> ${brainLabVisionTraceUsesNearest(trace.spec.signal) ? "nearest" : "aggregate"}</span>
    </div>
    <div class="brain-lab-world-probe-trace-list">
      ${topContributors || "<div>No visible contributors in the current probe view.</div>"}
    </div>
  `;
}

function drawBrainLabWorldProbeBiomeCues(context, scene, transform) {
  if (!scene?.focus || !scene.environment) {
    return;
  }

  const focusPoint = brainLabWorldProbeToScreen(scene.focus, transform);
  const focusRadius = Math.max(7, Number(scene.focus.radius || 0) * transform.scale + 5);
  const activeSamples = brainLabWorldProbeActiveBiomeSamples(scene);
  const currentColor = brainLabBiomeColor(activeSamples.currentBiomeKind);
  const environment = buildBrainLabWorldProbeEnvironmentPayload();

  drawBrainLabProbeBiomeHalo(context, focusPoint, focusRadius, currentColor);

  if (environment?.biomeBoundary) {
    drawBrainLabProbeBiomeBoundary(context, scene, transform, environment.biomeBoundary);
  } else if (environment?.biomeKind) {
    drawBrainLabProbeCircle(
      context,
      transform.centerX,
      transform.centerY,
      transform.radius * transform.scale + 4,
      brainLabBiomeColor(environment.biomeKind),
      [2, 6]);
  }
}

function drawBrainLabProbeBiomeHalo(context, point, radius, color) {
  context.save();
  context.strokeStyle = color;
  context.lineWidth = 3;
  context.globalAlpha = 0.8;
  context.beginPath();
  context.arc(point.x, point.y, radius, 0, Math.PI * 2);
  context.stroke();
  context.restore();
}

function drawBrainLabProbeBiomeBoundary(context, scene, transform, boundary) {
  const axis = brainLabWorldProbeScreenAxis(scene.focus.headingRadians, boundary.direction || "forward");
  const focusPoint = brainLabWorldProbeToScreen(scene.focus, transform);
  const offset = Number(boundary.offset || 0) * transform.scale;
  const origin = {
    x: focusPoint.x + axis.x * offset,
    y: focusPoint.y + axis.y * offset
  };
  const tangent = { x: -axis.y, y: axis.x };
  const lineLength = Math.max(transform.width, transform.height) * 1.4;
  const color = brainLabBiomeColor(boundary.farBiomeKind);

  context.save();
  context.strokeStyle = color;
  context.lineWidth = 2.5;
  context.globalAlpha = 0.75;
  context.setLineDash([8, 7]);
  context.beginPath();
  context.moveTo(origin.x - tangent.x * lineLength, origin.y - tangent.y * lineLength);
  context.lineTo(origin.x + tangent.x * lineLength, origin.y + tangent.y * lineLength);
  context.stroke();
  context.setLineDash([]);
  context.fillStyle = color;
  context.beginPath();
  context.moveTo(origin.x + axis.x * 28, origin.y + axis.y * 28);
  context.lineTo(origin.x + axis.x * 14 + tangent.x * 7, origin.y + axis.y * 14 + tangent.y * 7);
  context.lineTo(origin.x + axis.x * 14 - tangent.x * 7, origin.y + axis.y * 14 - tangent.y * 7);
  context.closePath();
  context.fill();
  drawBrainLabProbeBiomeBoundaryLabels(context, scene, origin, axis, tangent, boundary, color);
  context.restore();
}

function drawBrainLabProbeBiomeBoundaryLabels(context, scene, origin, axis, tangent, boundary, farColor) {
  const farLabel = formatEnumLabel(boundary.farBiomeKind || "override");
  const nearKind = brainLabWorldProbeBoundaryNearBiome(scene, boundary);
  const nearLabel = `${formatEnumLabel(nearKind)} side`;
  const farPoint = {
    x: origin.x + axis.x * 82 + tangent.x * 18,
    y: origin.y + axis.y * 82 + tangent.y * 18
  };
  const nearPoint = {
    x: origin.x - axis.x * 82 - tangent.x * 18,
    y: origin.y - axis.y * 82 - tangent.y * 18
  };
  drawBrainLabProbeBiomeChip(context, farPoint, `${farLabel} side`, farColor);
  drawBrainLabProbeBiomeChip(context, nearPoint, nearLabel, brainLabBiomeColor(nearKind));
}

function brainLabWorldProbeBoundaryNearBiome(scene, boundary) {
  return boundary.nearBiomeKind && boundary.nearBiomeKind !== "snapshot"
    ? boundary.nearBiomeKind
    : scene.environment?.currentBiomeKind || "Snapshot";
}

function drawBrainLabProbeBiomeChip(context, point, label, color) {
  context.save();
  context.globalAlpha = 1;
  context.font = "700 12px sans-serif";
  const paddingX = 8;
  const width = Math.ceil(context.measureText(label).width) + paddingX * 2 + 12;
  const height = 22;
  const x = point.x - width * 0.5;
  const y = point.y - height * 0.5;
  context.fillStyle = "rgba(255, 255, 255, 0.88)";
  context.strokeStyle = "rgba(80, 95, 115, 0.35)";
  context.lineWidth = 1;
  drawBrainLabRoundedRect(context, x, y, width, height, 5);
  context.fill();
  context.stroke();
  context.fillStyle = color;
  context.beginPath();
  context.arc(x + 11, y + height * 0.5, 4, 0, Math.PI * 2);
  context.fill();
  context.fillStyle = "#1f2a35";
  context.fillText(label, x + paddingX + 12, y + 15);
  context.restore();
}

function drawBrainLabRoundedRect(context, x, y, width, height, radius) {
  const r = Math.min(radius, width * 0.5, height * 0.5);
  context.beginPath();
  context.moveTo(x + r, y);
  context.lineTo(x + width - r, y);
  context.quadraticCurveTo(x + width, y, x + width, y + r);
  context.lineTo(x + width, y + height - r);
  context.quadraticCurveTo(x + width, y + height, x + width - r, y + height);
  context.lineTo(x + r, y + height);
  context.quadraticCurveTo(x, y + height, x, y + height - r);
  context.lineTo(x, y + r);
  context.quadraticCurveTo(x, y, x + r, y);
}

function brainLabWorldProbeHabitatProbeDistance(scene) {
  const senseRadius = Number(scene.senseRadius || 0);
  const bodyRadius = Number(scene.focus?.radius || 0);
  return Math.max(16, Math.min(80, Math.min(senseRadius * 0.25, bodyRadius * 8)));
}

function brainLabWorldProbeScreenAxis(headingRadians, direction) {
  const heading = Number(headingRadians || 0);
  const forward = { x: Math.cos(heading), y: -Math.sin(heading) };
  const right = { x: -Math.sin(heading), y: -Math.cos(heading) };
  if (direction === "behind") {
    return { x: -forward.x, y: -forward.y };
  }
  if (direction === "left") {
    return { x: -right.x, y: -right.y };
  }
  if (direction === "right") {
    return right;
  }
  return forward;
}

function brainLabBiomeColor(kind) {
  return brainLabBiomeColors[kind] || "#58ad57";
}

function drawBrainLabProbeDot(context, point, radius, fill, stroke) {
  context.save();
  context.fillStyle = fill;
  context.strokeStyle = stroke;
  context.lineWidth = 1;
  context.beginPath();
  context.arc(point.x, point.y, Math.min(12, radius), 0, Math.PI * 2);
  context.fill();
  context.stroke();
  context.restore();
}

function drawBrainLabProbeSound(context, point, creature) {
  const amplitude = Math.max(0, Number(creature.soundAmplitude || 0));
  const radius = 8 + amplitude * 22;
  context.save();
  context.strokeStyle = "rgba(218, 132, 39, 0.45)";
  context.lineWidth = 2;
  context.beginPath();
  context.arc(point.x, point.y, radius, 0, Math.PI * 2);
  context.stroke();
  context.restore();
}

function drawBrainLabProbeSmallPrey(context, point, prey, scale) {
  const bodyRadius = Math.max(3, Math.min(10, Number(prey.radius || 0) * scale));
  const heading = Number(prey.headingRadians || 0);
  const lineLength = bodyRadius + 5;
  context.save();
  context.fillStyle = "#c57a35";
  context.strokeStyle = "#7f481f";
  context.lineWidth = 1.25;
  context.beginPath();
  context.arc(point.x, point.y, bodyRadius, 0, Math.PI * 2);
  context.fill();
  context.stroke();
  context.strokeStyle = "#6a3916";
  context.lineWidth = 1.75;
  context.beginPath();
  context.moveTo(point.x, point.y);
  context.lineTo(point.x + Math.cos(heading) * lineLength, point.y - Math.sin(heading) * lineLength);
  context.stroke();
  context.restore();
  return bodyRadius;
}

function drawBrainLabProbeCreature(context, point, creature, scale) {
  const bodyRadius = Math.max(4, Math.min(14, Number(creature.radius || 0) * scale));
  const heading = Number(creature.headingRadians || 0);
  const lineLength = bodyRadius + 7;
  context.save();
  context.fillStyle = creature.isFocus ? "#1d2733" : "#5c7fbd";
  context.strokeStyle = creature.isFocus ? "#ffffff" : "#2e4d82";
  context.lineWidth = creature.isFocus ? 2.5 : 1.25;
  context.beginPath();
  context.arc(point.x, point.y, bodyRadius, 0, Math.PI * 2);
  context.fill();
  context.stroke();
  context.strokeStyle = creature.isFocus ? "#1d2733" : "#2e4d82";
  context.lineWidth = 2;
  context.beginPath();
  context.moveTo(point.x, point.y);
  context.lineTo(point.x + Math.cos(heading) * lineLength, point.y - Math.sin(heading) * lineLength);
  context.stroke();
  context.restore();
  return bodyRadius;
}

function addBrainLabWorldProbeHitTarget(target) {
  brainLabWorldProbeHitTargets.push({
    ...target,
    hitRadius: Math.max(10, Number(target.radius || 0) + 6)
  });
}

function drawBrainLabWorldProbeSelectedTarget(context) {
  if (!brainLabWorldProbeSelected) {
    return;
  }

  const target = brainLabWorldProbeHitTargets.find((candidate) =>
    candidate.type === brainLabWorldProbeSelected.type
    && String(candidate.id) === String(brainLabWorldProbeSelected.id));
  if (!target) {
    return;
  }

  context.save();
  context.strokeStyle = "#111820";
  context.lineWidth = 2.5;
  context.setLineDash([3, 3]);
  context.beginPath();
  context.arc(target.point.x, target.point.y, Math.max(12, target.radius + 7), 0, Math.PI * 2);
  context.stroke();
  context.restore();
}

function brainLabWorldProbePointFromEvent(event) {
  if (!brainLabWorldProbeCanvas) {
    return null;
  }

  const rect = brainLabWorldProbeCanvas.getBoundingClientRect();
  if (rect.width <= 0 || rect.height <= 0) {
    return null;
  }

  return {
    x: (event.clientX - rect.left) / rect.width * brainLabWorldProbeCanvas.width,
    y: (event.clientY - rect.top) / rect.height * brainLabWorldProbeCanvas.height
  };
}

function findBrainLabWorldProbeHitTarget(point) {
  if (!point) {
    return null;
  }

  return brainLabWorldProbeHitTargets
    .map((target) => {
      const dx = point.x - target.point.x;
      const dy = point.y - target.point.y;
      const distance = Math.hypot(dx, dy);
      return {
        target,
        distance,
        score: distance / Math.max(1, target.hitRadius)
      };
    })
    .filter((hit) => hit.distance <= hit.target.hitRadius)
    .sort((left, right) => left.score - right.score || right.target.priority - left.target.priority)
    .at(0)?.target ?? null;
}

function findBrainLabWorldProbeBoundaryHit(point) {
  const geometry = brainLabWorldProbeBoundaryGeometry();
  if (!point || !geometry) {
    return null;
  }

  const dx = point.x - geometry.origin.x;
  const dy = point.y - geometry.origin.y;
  const distanceFromLine = Math.abs(dx * geometry.axis.x + dy * geometry.axis.y);
  const distanceAlongLine = Math.abs(dx * geometry.tangent.x + dy * geometry.tangent.y);
  return distanceFromLine <= 9 && distanceAlongLine <= geometry.lineLength
    ? geometry
    : null;
}

function brainLabWorldProbeBoundaryGeometry() {
  const environment = buildBrainLabWorldProbeEnvironmentPayload();
  const boundary = environment?.biomeBoundary;
  if (!boundary || !brainLabWorldProbeScene?.focus) {
    return null;
  }

  const transform = brainLabWorldProbeTransform();
  const axis = brainLabWorldProbeScreenAxis(brainLabWorldProbeScene.focus.headingRadians, boundary.direction || "forward");
  const focusPoint = brainLabWorldProbeToScreen(brainLabWorldProbeScene.focus, transform);
  const offset = Number(boundary.offset || 0);
  const origin = {
    x: focusPoint.x + axis.x * offset * transform.scale,
    y: focusPoint.y + axis.y * offset * transform.scale
  };
  const tangent = { x: -axis.y, y: axis.x };
  return {
    boundary,
    transform,
    axis,
    origin,
    tangent,
    offset,
    lineLength: Math.max(transform.width, transform.height) * 1.4
  };
}

function selectBrainLabWorldProbeAtEvent(event) {
  if (brainLabWorldProbeSuppressClick) {
    brainLabWorldProbeSuppressClick = false;
    return;
  }

  const target = findBrainLabWorldProbeHitTarget(brainLabWorldProbePointFromEvent(event));
  brainLabWorldProbeSelected = target
    ? {
        type: target.type,
        id: target.id
      }
    : null;
  renderBrainLabWorldProbe();
}

function beginBrainLabWorldProbePan(event) {
  if (!brainLabWorldProbeScene || event.button !== 0) {
    return;
  }

  const point = brainLabWorldProbePointFromEvent(event);
  if (!point) {
    return;
  }

  const target = findBrainLabWorldProbeHitTarget(point);
  const boundaryHit = target ? null : findBrainLabWorldProbeBoundaryHit(point);
  if (brainLabWorldProbeTool.startsWith("add")) {
    const worldPoint = brainLabWorldProbeWorldFromCanvasPoint(point);
    addBrainLabWorldProbeItem(brainLabWorldProbeTool, worldPoint);
    event.preventDefault();
    return;
  }

  if (target && brainLabWorldProbeTool === "move" && target.type !== "focus" && target.type !== "sound") {
    const selection = {
      type: target.type,
      id: target.id
    };
    brainLabWorldProbeSelected = selection;
    brainLabWorldProbeDrag = {
      pointerId: event.pointerId,
      type: target.type,
      id: target.id,
      startX: point.x,
      startY: point.y,
      originX: Number(target.item.x || 0),
      originY: Number(target.item.y || 0),
      moved: false
    };
    event.preventDefault();
    brainLabWorldProbeCanvas.setPointerCapture?.(event.pointerId);
    brainLabWorldProbeCanvas.style.cursor = "grabbing";
    renderBrainLabWorldProbe();
    return;
  }

  if (target) {
    return;
  }

  if (boundaryHit) {
    event.preventDefault();
    brainLabWorldProbeBoundaryDrag = {
      pointerId: event.pointerId,
      startX: point.x,
      startY: point.y,
      axis: boundaryHit.axis,
      scale: boundaryHit.transform.scale,
      originOffset: boundaryHit.offset,
      moved: false
    };
    brainLabWorldProbeCanvas.setPointerCapture?.(event.pointerId);
    brainLabWorldProbeCanvas.style.cursor = "grabbing";
    return;
  }

  event.preventDefault();
  brainLabWorldProbeSuppressClick = false;
  brainLabWorldProbePan = {
    pointerId: event.pointerId,
    startX: point.x,
    startY: point.y,
    originX: brainLabWorldProbePanX,
    originY: brainLabWorldProbePanY,
    moved: false
  };
  brainLabWorldProbeCanvas.setPointerCapture?.(event.pointerId);
  brainLabWorldProbeCanvas.style.cursor = "grabbing";
}

function moveBrainLabWorldProbePointer(event) {
  if (brainLabWorldProbeDrag) {
    dragBrainLabWorldProbeItem(event);
    return;
  }

  if (brainLabWorldProbeBoundaryDrag) {
    dragBrainLabWorldProbeBoundary(event);
    return;
  }

  if (brainLabWorldProbePan) {
    panBrainLabWorldProbe(event);
    return;
  }

  updateBrainLabWorldProbeCursor(event);
}

function dragBrainLabWorldProbeBoundary(event) {
  const point = brainLabWorldProbePointFromEvent(event);
  if (!point || !brainLabWorldProbeBoundaryDrag) {
    return;
  }

  event.preventDefault();
  const dx = point.x - brainLabWorldProbeBoundaryDrag.startX;
  const dy = point.y - brainLabWorldProbeBoundaryDrag.startY;
  const screenDelta = dx * brainLabWorldProbeBoundaryDrag.axis.x + dy * brainLabWorldProbeBoundaryDrag.axis.y;
  if (Math.hypot(dx, dy) > 2) {
    brainLabWorldProbeBoundaryDrag.moved = true;
    brainLabWorldProbeSuppressClick = true;
  }

  const offset = brainLabWorldProbeBoundaryDrag.originOffset + screenDelta / Math.max(0.001, brainLabWorldProbeBoundaryDrag.scale);
  setBrainLabWorldProbeBoundaryOffset(offset, { evaluate: false });
  brainLabWorldProbeCanvas.style.cursor = "grabbing";
}

function setBrainLabWorldProbeBoundaryOffset(value, options = {}) {
  const { evaluate = true } = options;
  const offset = Math.max(-1000, Math.min(1000, Number(value) || 0));
  if (brainLabWorldProbeBoundaryOffsetInput) {
    brainLabWorldProbeBoundaryOffsetInput.value = formatBrainLabControlValue(offset);
  }
  if (brainLabWorldProbeEnvironmentSelect) {
    brainLabWorldProbeEnvironmentSelect.value = "custom";
  }
  clearBrainLabPopulation();
  renderBrainLabWorldProbe();
  updateBrainLabButtons();
  if (evaluate && brainLabWorldProbeScene && brainLabEvaluation) {
    scheduleBrainLabEvaluate(80);
  }
}

function panBrainLabWorldProbe(event) {
  const point = brainLabWorldProbePointFromEvent(event);
  if (!point) {
    return;
  }

  event.preventDefault();
  const dx = point.x - brainLabWorldProbePan.startX;
  const dy = point.y - brainLabWorldProbePan.startY;
  if (Math.hypot(dx, dy) > 3) {
    brainLabWorldProbePan.moved = true;
    brainLabWorldProbeSuppressClick = true;
  }

  brainLabWorldProbePanX = brainLabWorldProbePan.originX + dx;
  brainLabWorldProbePanY = brainLabWorldProbePan.originY + dy;
  renderBrainLabWorldProbe();
  brainLabWorldProbeCanvas.style.cursor = "grabbing";
}

function dragBrainLabWorldProbeItem(event) {
  const point = brainLabWorldProbePointFromEvent(event);
  if (!point || !brainLabWorldProbeDrag) {
    return;
  }

  event.preventDefault();
  const transform = brainLabWorldProbeTransform();
  const dx = (point.x - brainLabWorldProbeDrag.startX) / transform.scale;
  const dy = -(point.y - brainLabWorldProbeDrag.startY) / transform.scale;
  if (Math.hypot(point.x - brainLabWorldProbeDrag.startX, point.y - brainLabWorldProbeDrag.startY) > 2) {
    brainLabWorldProbeDrag.moved = true;
    brainLabWorldProbeSuppressClick = true;
  }

  const item = resolveBrainLabWorldProbeMutableItem(brainLabWorldProbeDrag.type, brainLabWorldProbeDrag.id);
  if (!item) {
    return;
  }

  item.x = brainLabWorldProbeDrag.originX + dx;
  item.y = brainLabWorldProbeDrag.originY + dy;
  updateBrainLabWorldProbeItemDistance(item);
  renderBrainLabWorldProbe();
  brainLabWorldProbeCanvas.style.cursor = "grabbing";
}

function resolveBrainLabWorldProbeMutableItem(type, id) {
  const numericId = Number(id);
  if (!brainLabWorldProbeScene) {
    return null;
  }

  if (type === "resource") {
    return (brainLabWorldProbeScene.resources || []).find((candidate) => Number(candidate.id) === numericId) ?? null;
  }
  if (type === "egg") {
    return (brainLabWorldProbeScene.eggs || []).find((candidate) => Number(candidate.id) === numericId) ?? null;
  }
  if (type === "smallPrey") {
    return (brainLabWorldProbeScene.smallPrey || []).find((candidate) => Number(candidate.id) === numericId) ?? null;
  }
  if (type === "creature") {
    return (brainLabWorldProbeScene.creatures || []).find((candidate) => Number(candidate.id) === numericId) ?? null;
  }

  return null;
}

function updateBrainLabWorldProbeItemDistance(item) {
  item.distance = Math.hypot(Number(item.x || 0), Number(item.y || 0));
}

function addBrainLabWorldProbeItem(tool, worldPoint) {
  if (!brainLabWorldProbeScene || !worldPoint) {
    return;
  }

  const id = brainLabWorldProbeNextSyntheticId--;
  const baseRadius = Math.max(3, Number(brainLabWorldProbeScene.focus?.radius || 6));
  if (tool === "addPlant" || tool === "addMeat") {
    const isPlant = tool === "addPlant";
    const resource = {
      id,
      kind: isPlant ? "Plant" : "Meat",
      plantKind: isPlant ? "Generic" : "",
      x: worldPoint.x,
      y: worldPoint.y,
      distance: 0,
      radius: isPlant ? Math.max(5, baseRadius * 0.7) : Math.max(4, baseRadius * 0.55),
      calories: isPlant ? 25 : 12,
      maxCalories: isPlant ? 25 : 12,
      freshness: 1
    };
    updateBrainLabWorldProbeItemDistance(resource);
    brainLabWorldProbeScene.resources = [...(brainLabWorldProbeScene.resources || []), resource];
    brainLabWorldProbeSelected = { type: "resource", id };
  } else if (tool === "addEgg") {
    const egg = {
      id,
      generation: Number(brainLabWorldProbeScene.focus?.generation || 0) + 1,
      x: worldPoint.x,
      y: worldPoint.y,
      distance: 0,
      radius: Math.max(4, baseRadius * 0.45),
      energy: 12,
      health: 1
    };
    updateBrainLabWorldProbeItemDistance(egg);
    brainLabWorldProbeScene.eggs = [...(brainLabWorldProbeScene.eggs || []), egg];
    brainLabWorldProbeSelected = { type: "egg", id };
  } else if (tool === "addSmallPrey") {
    const prey = {
      id,
      x: worldPoint.x,
      y: worldPoint.y,
      distance: 0,
      radius: Math.max(2, baseRadius * 0.25),
      calories: 16,
      maxCalories: 16,
      health: 0.2,
      maxHealth: 0.2,
      headingRadians: 0,
      speed: 0,
      ageSeconds: 0,
      isHeld: false,
      grabPressure: 0
    };
    updateBrainLabWorldProbeItemDistance(prey);
    brainLabWorldProbeScene.smallPrey = [...(brainLabWorldProbeScene.smallPrey || []), prey];
    brainLabWorldProbeSelected = { type: "smallPrey", id };
  } else if (tool === "addCreature" || tool === "addSound") {
    const soundOnly = tool === "addSound";
    const creature = {
      id,
      generation: Number(brainLabWorldProbeScene.focus?.generation || 0),
      brainArchitectureKind: soundOnly ? "ProbeSound" : "ProbeCreature",
      x: worldPoint.x,
      y: worldPoint.y,
      distance: 0,
      radius: soundOnly ? 0.5 : baseRadius,
      headingRadians: 0,
      energyRatio: 1,
      healthRatio: 1,
      hunger: 0,
      soundAmplitude: soundOnly ? 1 : 0,
      soundTone: 0,
      heardSound: false,
      soundDensity: 0,
      isFocus: false,
      isProbeSoundOnly: soundOnly
    };
    updateBrainLabWorldProbeItemDistance(creature);
    brainLabWorldProbeScene.creatures = [...(brainLabWorldProbeScene.creatures || []), creature];
    brainLabWorldProbeSelected = { type: soundOnly ? "sound" : "creature", id };
  }

  markBrainLabWorldProbeEdited();
}

function applyBrainLabWorldProbeFixture() {
  const fixture = selectedBrainLabWorldProbeFixture();
  if (!fixture || !brainLabWorldProbeScene) {
    return;
  }

  const editSet = fixture.worldProbe || {};
  resetBrainLabWorldProbeEdits({ restoreScene: false, clearSelection: true });
  brainLabWorldProbeScene.resources = (editSet.resources || []).map(brainLabWorldProbeFixtureResourceToScene);
  brainLabWorldProbeScene.eggs = (editSet.eggs || []).map(brainLabWorldProbeFixtureEggToScene);
  brainLabWorldProbeScene.smallPrey = (editSet.smallPrey || []).map(brainLabWorldProbeFixtureSmallPreyToScene);
  brainLabWorldProbeScene.creatures = (editSet.creatures || []).map(brainLabWorldProbeFixtureCreatureToScene);
  brainLabWorldProbeNextSyntheticId = nextBrainLabWorldProbeSyntheticId();
  brainLabWorldProbeEdited = true;
  clearBrainLabPopulation();
  renderBrainLabWorldProbe();
  evaluateBrainLab();
}

async function saveBrainLabWorldProbeFixture() {
  if (!brainLabWorldProbeScene) {
    return;
  }

  const name = prompt("Save probe setup as", "New Probe Setup");
  if (!name?.trim()) {
    return;
  }

  const payload = {
    name: name.trim(),
    description: "",
    tags: [],
    worldProbe: buildBrainLabWorldProbeFixturePayload()
  };
  brainLabStatus.textContent = "Saving probe setup";
  const response = await fetch("/api/brain-lab/probe-fixtures", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Probe setup save failed.");
    return;
  }

  const result = await response.json();
  await loadBrainLabWorldProbeFixtures(result.fixture?.path || "");
  brainLabStatus.textContent = `Saved probe setup ${result.fixture?.name || name.trim()}.`;
}

async function deleteBrainLabWorldProbeFixture() {
  const fixture = selectedBrainLabWorldProbeFixture();
  if (!fixture?.canDelete) {
    return;
  }

  if (!confirm(`Delete probe setup "${fixture.name}"?`)) {
    return;
  }

  brainLabStatus.textContent = "Deleting probe setup";
  const response = await fetch(`/api/brain-lab/probe-fixtures?path=${encodeURIComponent(fixture.path)}`, {
    method: "DELETE"
  });
  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Probe setup delete failed.");
    return;
  }

  const result = await response.json();
  await loadBrainLabWorldProbeFixtures();
  brainLabStatus.textContent = `Archived probe setup to ${result.archivedPath}.`;
}

function buildBrainLabWorldProbeFixturePayload() {
  let nextId = -1;
  const resources = (brainLabWorldProbeScene.resources || [])
    .filter(isBrainLabWorldProbeResourceVisible)
    .map((resource) => {
      const local = brainLabWorldProbeWorldToFixturePoint(resource);
      return {
        id: nextId--,
        kind: resource.kind || "Plant",
        plantKind: resource.plantKind || "",
        x: local.x,
        y: local.y,
        radius: Number(resource.radius || 1),
        calories: Number(resource.calories || 0),
        maxCalories: Number(resource.maxCalories || resource.calories || 1),
        freshness: Number(resource.freshness ?? 1)
      };
    });
  const eggs = (brainLabWorldProbeScene.eggs || [])
    .filter(isBrainLabWorldProbeEggVisible)
    .map((egg) => {
      const local = brainLabWorldProbeWorldToFixturePoint(egg);
      return {
        id: nextId--,
        generation: Number(egg.generation || 0),
        x: local.x,
        y: local.y,
        radius: Number(egg.radius || 1),
        energy: Number(egg.energy || 0),
        health: Number(egg.health || 1)
      };
    });
  const smallPrey = (brainLabWorldProbeScene.smallPrey || [])
    .filter(isBrainLabWorldProbeSmallPreyVisible)
    .map((prey) => {
      const local = brainLabWorldProbeWorldToFixturePoint(prey);
      return {
        id: nextId--,
        x: local.x,
        y: local.y,
        radius: Number(prey.radius || 1),
        calories: Number(prey.calories || 0),
        maxCalories: Number(prey.maxCalories || prey.calories || 1),
        health: Number(prey.health || 0.2),
        maxHealth: Number(prey.maxHealth || prey.health || 0.2),
        headingRadians: normalizeBrainLabWorldProbeRadians(Number(prey.headingRadians || 0) - brainLabWorldProbeFocusHeading()),
        speed: Number(prey.speed || 0),
        grabPressure: Number(prey.grabPressure || 0)
      };
    });
  const creatures = (brainLabWorldProbeScene.creatures || [])
    .filter(isBrainLabWorldProbeCreatureVisible)
    .map((creature) => {
      const local = brainLabWorldProbeWorldToFixturePoint(creature);
      return {
        id: nextId--,
        generation: Number(creature.generation || 0),
        brainArchitectureKind: creature.brainArchitectureKind || "",
        x: local.x,
        y: local.y,
        radius: Number(creature.radius || 1),
        headingRadians: normalizeBrainLabWorldProbeRadians(Number(creature.headingRadians || 0) - brainLabWorldProbeFocusHeading()),
        energyRatio: Number(creature.energyRatio ?? 1),
        healthRatio: Number(creature.healthRatio ?? 1),
        hunger: Number(creature.hunger || 0),
        soundAmplitude: brainLabWorldProbeMutedSoundKeys.has(brainLabWorldProbeSoundKey(creature.id))
          ? 0
          : Number(creature.soundAmplitude || 0),
        soundTone: Number(creature.soundTone || 0),
        isProbeSoundOnly: Boolean(creature.isProbeSoundOnly)
      };
    });

  return { resources, eggs, creatures, smallPrey };
}

function brainLabWorldProbeFixtureResourceToScene(resource) {
  const point = brainLabWorldProbeFixtureToWorldPoint(resource);
  const sceneResource = {
    id: Number(resource.id),
    kind: resource.kind || "Plant",
    plantKind: resource.plantKind || "",
    x: point.x,
    y: point.y,
    distance: 0,
    radius: Number(resource.radius || 1),
    calories: Number(resource.calories || 0),
    maxCalories: Number(resource.maxCalories || resource.calories || 1),
    freshness: Number(resource.freshness ?? 1)
  };
  updateBrainLabWorldProbeItemDistance(sceneResource);
  return sceneResource;
}

function brainLabWorldProbeFixtureEggToScene(egg) {
  const point = brainLabWorldProbeFixtureToWorldPoint(egg);
  const sceneEgg = {
    id: Number(egg.id),
    generation: Number(egg.generation || Number(brainLabWorldProbeScene?.focus?.generation || 0) + 1),
    x: point.x,
    y: point.y,
    distance: 0,
    radius: Number(egg.radius || 1),
    energy: Number(egg.energy || 0),
    health: Number(egg.health || 1)
  };
  updateBrainLabWorldProbeItemDistance(sceneEgg);
  return sceneEgg;
}

function brainLabWorldProbeFixtureSmallPreyToScene(prey) {
  const point = brainLabWorldProbeFixtureToWorldPoint(prey);
  const scenePrey = {
    id: Number(prey.id),
    x: point.x,
    y: point.y,
    distance: 0,
    radius: Number(prey.radius || 1),
    calories: Number(prey.calories || 0),
    maxCalories: Number(prey.maxCalories || prey.calories || 1),
    health: Number(prey.health || 0.2),
    maxHealth: Number(prey.maxHealth || prey.health || 0.2),
    headingRadians: normalizeBrainLabWorldProbeRadians(Number(prey.headingRadians || 0) + brainLabWorldProbeFocusHeading()),
    speed: Number(prey.speed || 0),
    ageSeconds: 0,
    isHeld: false,
    grabPressure: Number(prey.grabPressure || 0)
  };
  updateBrainLabWorldProbeItemDistance(scenePrey);
  return scenePrey;
}

function brainLabWorldProbeFixtureCreatureToScene(creature) {
  const point = brainLabWorldProbeFixtureToWorldPoint(creature);
  const sceneCreature = {
    id: Number(creature.id),
    generation: Number(creature.generation || brainLabWorldProbeScene?.focus?.generation || 0),
    brainArchitectureKind: creature.brainArchitectureKind || "ProbeCreature",
    x: point.x,
    y: point.y,
    distance: 0,
    radius: Number(creature.radius || 1),
    headingRadians: normalizeBrainLabWorldProbeRadians(Number(creature.headingRadians || 0) + brainLabWorldProbeFocusHeading()),
    energyRatio: Number(creature.energyRatio ?? 1),
    healthRatio: Number(creature.healthRatio ?? 1),
    hunger: Number(creature.hunger || 0),
    soundAmplitude: Number(creature.soundAmplitude || 0),
    soundTone: Number(creature.soundTone || 0),
    heardSound: false,
    soundDensity: 0,
    isFocus: false,
    isProbeSoundOnly: Boolean(creature.isProbeSoundOnly)
  };
  updateBrainLabWorldProbeItemDistance(sceneCreature);
  return sceneCreature;
}

function brainLabWorldProbeFixtureToWorldPoint(item) {
  const heading = brainLabWorldProbeFocusHeading();
  const forward = Number(item?.x || 0);
  const right = Number(item?.y || 0);
  return {
    x: forward * Math.cos(heading) + right * Math.sin(heading),
    y: forward * Math.sin(heading) - right * Math.cos(heading)
  };
}

function brainLabWorldProbeWorldToFixturePoint(item) {
  const heading = brainLabWorldProbeFocusHeading();
  const x = Number(item?.x || 0);
  const y = Number(item?.y || 0);
  return {
    x: x * Math.cos(heading) + y * Math.sin(heading),
    y: x * Math.sin(heading) - y * Math.cos(heading)
  };
}

function brainLabWorldProbeFocusHeading() {
  return Number(brainLabWorldProbeScene?.focus?.headingRadians || 0);
}

function normalizeBrainLabWorldProbeRadians(radians) {
  const value = Number(radians || 0);
  if (!Number.isFinite(value)) {
    return 0;
  }

  return Math.atan2(Math.sin(value), Math.cos(value));
}

function nextBrainLabWorldProbeSyntheticId() {
  const ids = [
    ...(brainLabWorldProbeScene?.resources || []).map((item) => Number(item.id)),
    ...(brainLabWorldProbeScene?.eggs || []).map((item) => Number(item.id)),
    ...(brainLabWorldProbeScene?.smallPrey || []).map((item) => Number(item.id)),
    ...(brainLabWorldProbeScene?.creatures || []).map((item) => Number(item.id))
  ].filter(Number.isFinite);
  return Math.min(-1, ...ids) - 1;
}

function endBrainLabWorldProbePan(event) {
  if (brainLabWorldProbeDrag) {
    if (brainLabWorldProbeDrag.pointerId === event.pointerId) {
      brainLabWorldProbeCanvas.releasePointerCapture?.(event.pointerId);
      const moved = brainLabWorldProbeDrag.moved;
      brainLabWorldProbeDrag = null;
      updateBrainLabWorldProbeCursor(event);
      if (moved) {
        markBrainLabWorldProbeEdited();
      }
    }
    return;
  }

  if (brainLabWorldProbeBoundaryDrag) {
    if (brainLabWorldProbeBoundaryDrag.pointerId === event.pointerId) {
      brainLabWorldProbeCanvas.releasePointerCapture?.(event.pointerId);
      const moved = brainLabWorldProbeBoundaryDrag.moved;
      brainLabWorldProbeBoundaryDrag = null;
      updateBrainLabWorldProbeCursor(event);
      if (moved) {
        scheduleBrainLabEvaluate(80);
      }
    }
    return;
  }

  if (!brainLabWorldProbePan) {
    return;
  }

  if (brainLabWorldProbePan.pointerId === event.pointerId) {
    brainLabWorldProbeCanvas.releasePointerCapture?.(event.pointerId);
    brainLabWorldProbePan = null;
    updateBrainLabWorldProbeCursor(event);
    updateBrainLabWorldProbeZoomControls();
  }
}

function updateBrainLabWorldProbeCursor(event) {
  if (!brainLabWorldProbeCanvas) {
    return;
  }

  const point = brainLabWorldProbePointFromEvent(event);
  const target = findBrainLabWorldProbeHitTarget(point);
  const boundaryHit = target ? null : findBrainLabWorldProbeBoundaryHit(point);
  if (!brainLabWorldProbeScene) {
    brainLabWorldProbeCanvas.style.cursor = "default";
  } else if (brainLabWorldProbeTool.startsWith("add")) {
    brainLabWorldProbeCanvas.style.cursor = "crosshair";
  } else if (brainLabWorldProbeTool === "move" && target && target.type !== "focus" && target.type !== "sound") {
    brainLabWorldProbeCanvas.style.cursor = "grab";
  } else if (boundaryHit) {
    brainLabWorldProbeCanvas.style.cursor = "grab";
  } else {
    brainLabWorldProbeCanvas.style.cursor = target ? "pointer" : "grab";
  }
}

function setBrainLabWorldProbeTool(tool) {
  brainLabWorldProbeTool = tool || "select";
  for (const button of brainLabWorldProbeToolButtons || []) {
    button.classList.toggle("is-active", button.dataset.brainLabWorldTool === brainLabWorldProbeTool);
  }
  if (brainLabWorldProbeCanvas) {
    brainLabWorldProbeCanvas.style.cursor = brainLabWorldProbeTool.startsWith("add") ? "crosshair" : "default";
  }
}

function resetBrainLabWorldProbeEdits(options = {}) {
  const { restoreScene = true, clearSelection = true } = options;
  brainLabWorldProbeHiddenKeys = new Set();
  brainLabWorldProbeMutedSoundKeys = new Set();
  brainLabWorldProbeEdited = false;
  brainLabWorldProbeNextSyntheticId = -1;
  if (restoreScene && brainLabWorldProbeBaseScene) {
    brainLabWorldProbeScene = cloneBrainLabWorldProbeScene(brainLabWorldProbeBaseScene);
  }
  if (clearSelection) {
    brainLabWorldProbeSelected = null;
  }
  brainLabWorldProbeHitTargets = [];
}

function resetBrainLabWorldProbeZoom(shouldRender = true) {
  brainLabWorldProbePanX = 0;
  brainLabWorldProbePanY = 0;
  brainLabWorldProbePan = null;
  setBrainLabWorldProbeZoom(1, shouldRender);
}

function changeBrainLabWorldProbeZoom(multiplier) {
  setBrainLabWorldProbeZoom(brainLabWorldProbeZoom * multiplier);
}

function setBrainLabWorldProbeZoom(value, shouldRender = true) {
  brainLabWorldProbeZoom = Math.max(0.4, Math.min(5, Number(value) || 1));
  updateBrainLabWorldProbeZoomControls();
  if (shouldRender) {
    renderBrainLabWorldProbe();
  }
}

function updateBrainLabWorldProbeZoomControls() {
  const hasScene = Boolean(brainLabWorldProbeScene);
  if (brainLabWorldProbeZoomStatus) {
    brainLabWorldProbeZoomStatus.textContent = `${Math.round(brainLabWorldProbeZoom * 100)}%`;
  }
  if (brainLabWorldProbeZoomOutButton) {
    brainLabWorldProbeZoomOutButton.disabled = !hasScene || brainLabWorldProbeZoom <= 0.401;
  }
  if (brainLabWorldProbeZoomInButton) {
    brainLabWorldProbeZoomInButton.disabled = !hasScene || brainLabWorldProbeZoom >= 4.999;
  }
  if (brainLabWorldProbeZoomResetButton) {
    brainLabWorldProbeZoomResetButton.disabled = !hasScene
      || (Math.abs(brainLabWorldProbeZoom - 1) < 0.001
        && Math.abs(brainLabWorldProbePanX) < 0.5
        && Math.abs(brainLabWorldProbePanY) < 0.5);
  }
}

function zoomBrainLabWorldProbeWithWheel(event) {
  if (!brainLabWorldProbeScene) {
    return;
  }

  event.preventDefault();
  changeBrainLabWorldProbeZoom(event.deltaY < 0 ? 1.12 : 1 / 1.12);
}

function clearBrainLabWorldProbeEdits() {
  resetBrainLabWorldProbeEdits();
  resetBrainLabWorldProbeEnvironment();
  clearBrainLabPopulation();
  renderBrainLabWorldProbe();
  evaluateBrainLab();
}

function hideSelectedBrainLabWorldProbeItem() {
  const selection = resolveBrainLabWorldProbeSelection();
  if (!selection || selection.type === "focus") {
    return;
  }

  brainLabWorldProbeHiddenKeys.add(selection.type === "sound"
    ? brainLabWorldProbeObjectKey("creature", selection.id)
    : selection.key);
  markBrainLabWorldProbeEdited();
}

function muteSelectedBrainLabWorldProbeSound() {
  const selection = resolveBrainLabWorldProbeSelection();
  if (!selection || !brainLabWorldProbeSelectionHasSound(selection)) {
    return;
  }

  brainLabWorldProbeMutedSoundKeys.add(brainLabWorldProbeSoundKey(selection.id));
  markBrainLabWorldProbeEdited();
}

function renderBrainLabWorldProbeSelection() {
  if (!brainLabWorldProbeSelection) {
    return;
  }

  const selection = resolveBrainLabWorldProbeSelection();
  if (!selection) {
    brainLabWorldProbeSelection.textContent = "No selection.";
  } else {
    const details = brainLabWorldProbeSelectionDetails(selection);
    brainLabWorldProbeSelection.innerHTML = `<strong>${escapeHtml(details.title)}</strong><code>${escapeHtml(details.key)}</code>${details.facts.length > 0 ? ` ${details.facts.map(escapeHtml).join(" | ")}` : ""}`;
  }

  renderBrainLabWorldProbeSelectionEditor(selection);
  updateBrainLabWorldProbeActionButtons(selection);
}

function renderBrainLabWorldProbeSelectionEditor(selection) {
  if (!brainLabWorldProbeEditor) {
    return;
  }

  if (!selection) {
    brainLabWorldProbeEditor.classList.add("empty");
    brainLabWorldProbeEditor.textContent = "Select an item to edit.";
    return;
  }

  const deleted = isBrainLabWorldProbeSelectionDeleted(selection);
  const fields = brainLabWorldProbeEditorFields(selection);
  const note = brainLabWorldProbeEditorNote(selection, deleted);
  if (fields.length === 0) {
    brainLabWorldProbeEditor.classList.add("empty");
    brainLabWorldProbeEditor.textContent = note || "This selection has no editable world properties.";
    return;
  }

  brainLabWorldProbeEditor.classList.remove("empty");
  brainLabWorldProbeEditor.innerHTML = [
    ...fields.map((field) => renderBrainLabWorldProbeEditorField(field, deleted)),
    note ? `<div class="brain-lab-world-probe-editor-note">${escapeHtml(note)}</div>` : ""
  ].join("");
}

function brainLabWorldProbeEditorFields(selection) {
  if (!selection || selection.type === "focus") {
    return [];
  }

  const item = selection.item;
  const radius = Math.max(1, Number(brainLabWorldProbeScene?.probeRadius || 1));
  const fields = [
    brainLabWorldProbeNumberField("x", "X", item.x, -radius, radius, 1),
    brainLabWorldProbeNumberField("y", "Y", item.y, -radius, radius, 1)
  ];

  if (selection.type === "resource") {
    fields.push(brainLabWorldProbeNumberField("radius", "Radius", item.radius, 0.1, radius, 0.1));
    if (item.kind === "Plant") {
      fields.push({
        key: "plantKind",
        label: "Plant type",
        type: "select",
        value: item.plantKind || "Generic",
        options: ["Generic", "Tender", "Rich", "Tough"]
      });
    }
    fields.push(brainLabWorldProbeNumberField("calories", "Calories", item.calories, 0, 100000, 0.1));
    fields.push(brainLabWorldProbeNumberField("maxCalories", "Max kcal", item.maxCalories, 0.1, 100000, 0.1));
    if (item.kind === "Meat") {
      fields.push(brainLabWorldProbeRangeField("freshness", "Freshness", item.freshness ?? 1, 0, 1, 0.01));
    }
    return fields;
  }

  if (selection.type === "egg") {
    fields.push(brainLabWorldProbeNumberField("generation", "Generation", item.generation, 0, 1000000, 1));
    fields.push(brainLabWorldProbeNumberField("radius", "Radius", item.radius, 0.1, radius, 0.1));
    fields.push(brainLabWorldProbeNumberField("energy", "Energy", item.energy, 0, 100000, 0.1));
    fields.push(brainLabWorldProbeNumberField("health", "Health", item.health, 0, 100000, 0.1));
    return fields;
  }

  if (selection.type === "smallPrey") {
    fields.push(brainLabWorldProbeNumberField("radius", "Radius", item.radius, 0.1, radius, 0.1));
    fields.push(brainLabWorldProbeNumberField("headingDegrees", "Heading", radiansToDegrees(item.headingRadians), -180, 180, 1));
    fields.push(brainLabWorldProbeNumberField("speed", "Speed", item.speed || 0, 0, 1000, 0.1));
    fields.push(brainLabWorldProbeNumberField("calories", "Calories", item.calories, 0, 100000, 0.1));
    fields.push(brainLabWorldProbeNumberField("maxCalories", "Max kcal", item.maxCalories, 0.1, 100000, 0.1));
    fields.push(brainLabWorldProbeNumberField("health", "Health", item.health, 0, 100000, 0.01));
    fields.push(brainLabWorldProbeNumberField("maxHealth", "Max health", item.maxHealth, 0.01, 100000, 0.01));
    fields.push(brainLabWorldProbeRangeField("grabPressure", "Grab pressure", item.grabPressure || 0, 0, 1, 0.01));
    return fields;
  }

  if (selection.type === "creature" || selection.type === "sound") {
    if (selection.type === "creature") {
      fields.push(brainLabWorldProbeNumberField("generation", "Generation", item.generation, 0, 1000000, 1));
      fields.push(brainLabWorldProbeNumberField("radius", "Radius", item.radius, 0.1, radius, 0.1));
      fields.push(brainLabWorldProbeNumberField("headingDegrees", "Heading", radiansToDegrees(item.headingRadians), -180, 180, 1));
      fields.push(brainLabWorldProbeRangeField("energyRatio", "Energy", item.energyRatio ?? 1, 0, 1, 0.01));
      fields.push(brainLabWorldProbeRangeField("healthRatio", "Health", item.healthRatio ?? 1, 0, 1, 0.01));
    }
    fields.push(brainLabWorldProbeRangeField("soundAmplitude", "Sound amp", item.soundAmplitude || 0, 0, 1, 0.01));
    fields.push(brainLabWorldProbeRangeField("soundTone", "Sound tone", item.soundTone || 0, -1, 1, 0.01));
    return fields;
  }

  return fields;
}

function brainLabWorldProbeNumberField(key, label, value, min, max, step) {
  return {
    key,
    label,
    type: "number",
    value,
    min,
    max,
    step
  };
}

function brainLabWorldProbeRangeField(key, label, value, min, max, step) {
  return {
    key,
    label,
    type: "range",
    value,
    min,
    max,
    step
  };
}

function renderBrainLabWorldProbeEditorField(field, disabled) {
  const value = clampBrainLabWorldProbeEditorValue(field.value, field);
  const disabledAttribute = disabled ? " disabled" : "";
  if (field.type === "select") {
    const options = (field.options || [])
      .map((option) => `<option value="${escapeHtml(option)}"${option === field.value ? " selected" : ""}>${escapeHtml(formatEnumLabel(option))}</option>`)
      .join("");
    return `
      <div class="brain-lab-world-probe-editor-field">
        <label for="brain-lab-world-probe-${escapeHtml(field.key)}">${escapeHtml(field.label)}</label>
        <select id="brain-lab-world-probe-${escapeHtml(field.key)}" data-brain-lab-world-edit-field="${escapeHtml(field.key)}"${disabledAttribute}>${options}</select>
      </div>
    `;
  }

  const min = Number(field.min);
  const max = Number(field.max);
  const step = Number(field.step);
  const formatted = formatBrainLabControlValue(value);
  if (field.type === "range") {
    return `
      <div class="brain-lab-world-probe-editor-field is-range">
        <label for="brain-lab-world-probe-${escapeHtml(field.key)}-range">${escapeHtml(field.label)}</label>
        <input id="brain-lab-world-probe-${escapeHtml(field.key)}-range" data-brain-lab-world-edit-field="${escapeHtml(field.key)}" type="range" min="${min}" max="${max}" step="${step}" value="${formatted}"${disabledAttribute}>
        <input data-brain-lab-world-edit-field="${escapeHtml(field.key)}" type="number" min="${min}" max="${max}" step="${step}" value="${formatted}"${disabledAttribute}>
      </div>
    `;
  }

  return `
    <div class="brain-lab-world-probe-editor-field">
      <label for="brain-lab-world-probe-${escapeHtml(field.key)}">${escapeHtml(field.label)}</label>
      <input id="brain-lab-world-probe-${escapeHtml(field.key)}" data-brain-lab-world-edit-field="${escapeHtml(field.key)}" type="number" min="${min}" max="${max}" step="${step}" value="${formatted}"${disabledAttribute}>
    </div>
  `;
}

function brainLabWorldProbeEditorNote(selection, deleted) {
  if (deleted) {
    return "Deleted items are excluded from recomputation until edits are cleared.";
  }
  if (!selection) {
    return "";
  }
  if (selection.type === "focus") {
    return "The selected creature is the probe anchor. Edit its brain inputs in the Inputs panel below.";
  }
  if (selection.type === "creature") {
    const hunger = Math.max(0, Math.min(1, 1 - Number(selection.item.energyRatio || 0)));
    return `Hunger follows energy here; current derived hunger is ${formatPercent(hunger)}.`;
  }
  if (selection.type === "sound") {
    return "Sound edits use the source creature position and emitted tone.";
  }
  if (selection.type === "smallPrey") {
    return "Small prey is sensed as live fresh meat and generic food.";
  }
  return "";
}

function updateBrainLabWorldProbeEditorField(control) {
  const selection = resolveBrainLabWorldProbeSelection();
  if (!selection || isBrainLabWorldProbeSelectionDeleted(selection)) {
    return;
  }

  const fieldKey = control.dataset.brainLabWorldEditField;
  const field = brainLabWorldProbeEditorFields(selection).find((candidate) => candidate.key === fieldKey);
  if (!field) {
    return;
  }

  const item = selection.item;
  if (field.type === "select") {
    item[fieldKey] = control.value;
  } else {
    const rawValue = Number(control.value);
    if (!Number.isFinite(rawValue)) {
      return;
    }
    const value = clampBrainLabWorldProbeEditorValue(rawValue, field);
    if (fieldKey === "headingDegrees") {
      item.headingRadians = degreesToRadians(value);
    } else {
      item[fieldKey] = value;
    }
  }

  if (fieldKey === "x" || fieldKey === "y") {
    updateBrainLabWorldProbeItemDistance(item);
  }
  if (fieldKey === "calories" && Number(item.maxCalories || 0) < Number(item.calories || 0)) {
    item.maxCalories = Number(item.calories || 0);
  }
  if (fieldKey === "maxCalories" && Number(item.maxCalories || 0) < Number(item.calories || 0)) {
    item.maxCalories = Number(item.calories || 0);
  }
  if (fieldKey === "health" && Number(item.maxHealth || 0) < Number(item.health || 0)) {
    item.maxHealth = Number(item.health || 0);
  }
  if (fieldKey === "maxHealth" && Number(item.maxHealth || 0) < Number(item.health || 0)) {
    item.maxHealth = Number(item.health || 0);
  }
  if (fieldKey === "soundAmplitude" && Number(item.soundAmplitude || 0) > 0.05) {
    brainLabWorldProbeMutedSoundKeys.delete(brainLabWorldProbeSoundKey(selection.id));
  }

  markBrainLabWorldProbeEdited();
}

function clampBrainLabWorldProbeEditorValue(value, field) {
  const number = Number(value);
  if (!Number.isFinite(number)) {
    return Number(field.min || 0);
  }

  return Math.min(Math.max(number, Number(field.min)), Number(field.max));
}

function radiansToDegrees(radians) {
  const degrees = Number(radians || 0) * 180 / Math.PI;
  if (!Number.isFinite(degrees)) {
    return 0;
  }

  return ((degrees + 180) % 360 + 360) % 360 - 180;
}

function degreesToRadians(degrees) {
  return Number(degrees || 0) * Math.PI / 180;
}

function updateBrainLabWorldProbeActionButtons(selection = resolveBrainLabWorldProbeSelection()) {
  const hasEdits = brainLabWorldProbeEdited
    || brainLabWorldProbeHiddenKeys.size > 0
    || brainLabWorldProbeMutedSoundKeys.size > 0
    || isBrainLabWorldProbeEnvironmentActive();
  if (hideBrainLabWorldProbeSelectionButton) {
    hideBrainLabWorldProbeSelectionButton.disabled = !selection
      || selection.type === "focus"
      || brainLabWorldProbeHiddenKeys.has(selection.type === "sound"
        ? brainLabWorldProbeObjectKey("creature", selection.id)
        : selection.key);
  }
  if (muteBrainLabWorldProbeSelectionSoundButton) {
    muteBrainLabWorldProbeSelectionSoundButton.disabled = !selection
      || !brainLabWorldProbeSelectionHasSound(selection)
      || brainLabWorldProbeHiddenKeys.has(selection.type === "sound"
        ? brainLabWorldProbeObjectKey("creature", selection.id)
        : selection.key)
      || brainLabWorldProbeMutedSoundKeys.has(brainLabWorldProbeSoundKey(selection.id));
  }
  if (clearBrainLabWorldProbeEditsButton) {
    clearBrainLabWorldProbeEditsButton.disabled = !hasEdits;
  }
}

function resolveBrainLabWorldProbeSelection() {
  if (!brainLabWorldProbeScene || !brainLabWorldProbeSelected) {
    return null;
  }

  const type = brainLabWorldProbeSelected.type;
  const id = Number(brainLabWorldProbeSelected.id);
  let item = null;
  let key = "";
  if (type === "resource") {
    item = (brainLabWorldProbeScene.resources || []).find((candidate) => Number(candidate.id) === id) ?? null;
    key = brainLabWorldProbeObjectKey("resource", id);
  } else if (type === "egg") {
    item = (brainLabWorldProbeScene.eggs || []).find((candidate) => Number(candidate.id) === id) ?? null;
    key = brainLabWorldProbeObjectKey("egg", id);
  } else if (type === "smallPrey") {
    item = (brainLabWorldProbeScene.smallPrey || []).find((candidate) => Number(candidate.id) === id) ?? null;
    key = brainLabWorldProbeObjectKey("smallPrey", id);
  } else if (type === "creature" || type === "sound") {
    item = (brainLabWorldProbeScene.creatures || []).find((candidate) => Number(candidate.id) === id) ?? null;
    key = type === "sound"
      ? brainLabWorldProbeSoundKey(id)
      : brainLabWorldProbeObjectKey("creature", id);
  } else if (type === "focus") {
    item = Number(brainLabWorldProbeScene.focus?.id) === id ? brainLabWorldProbeScene.focus : null;
    key = brainLabWorldProbeObjectKey("focus", id);
  }

  return item ? { type, id, key, item } : null;
}

function brainLabWorldProbeSelectionDetails(selection) {
  const item = selection.item;
  const facts = [];
  if (Number.isFinite(Number(item.distance))) {
    facts.push(`${formatBrainLabNumber(item.distance)}u away`);
  }

  if (selection.type === "resource") {
    facts.push(`${formatBrainLabNumber(item.calories)} kcal`);
    if (item.kind === "Plant" && item.plantKind) {
      facts.push(formatEnumLabel(item.plantKind));
    }
    if (item.kind === "Meat") {
      facts.push(`fresh ${formatPercent(item.freshness)}`);
    }
  } else if (selection.type === "egg") {
    facts.push(`gen ${formatNumber(item.generation)}`);
    facts.push(`${formatBrainLabNumber(item.energy)} kcal`);
  } else if (selection.type === "smallPrey") {
    facts.push(`${formatBrainLabNumber(item.calories)} kcal`);
    facts.push(`health ${formatBrainLabNumber(item.health)}`);
    facts.push(`speed ${formatBrainLabNumber(item.speed || 0)}`);
    if (item.isHeld) {
      facts.push("held");
    }
  } else if (selection.type === "creature" || selection.type === "focus" || selection.type === "sound") {
    facts.push(`gen ${formatNumber(item.generation)}`);
    facts.push(`energy ${formatPercent(item.energyRatio)}`);
    facts.push(`sound ${formatBrainLabNumber(item.soundAmplitude)}`);
  }

  if (brainLabWorldProbeHiddenKeys.has(selection.key)
    || (selection.type === "sound" && brainLabWorldProbeHiddenKeys.has(brainLabWorldProbeObjectKey("creature", selection.id)))) {
    facts.push("deleted");
  }
  if (brainLabWorldProbeMutedSoundKeys.has(brainLabWorldProbeSoundKey(selection.id))) {
    facts.push("muted");
  }

  return {
    title: brainLabWorldProbeSelectionTitle(selection),
    key: selection.key,
    facts
  };
}

function brainLabWorldProbeSelectionTitle(selection) {
  if (selection.type === "resource") {
    return selection.item.kind === "Plant"
      ? `Plant #${formatNumber(selection.id)}`
      : `Meat #${formatNumber(selection.id)}`;
  }
  if (selection.type === "egg") {
    return `Egg #${formatNumber(selection.id)}`;
  }
  if (selection.type === "smallPrey") {
    return `Small prey #${formatNumber(selection.id)}`;
  }
  if (selection.type === "creature") {
    return `Creature #${formatNumber(selection.id)}`;
  }
  if (selection.type === "sound") {
    return `Sound #${formatNumber(selection.id)}`;
  }
  return `Selected creature #${formatNumber(selection.id)}`;
}

function brainLabWorldProbeSelectionHasSound(selection) {
  return (selection.type === "creature" || selection.type === "sound")
    && Number(selection.item.soundAmplitude || 0) > 0.05;
}

function isBrainLabWorldProbeSelectionDeleted(selection) {
  if (!selection) {
    return false;
  }

  return brainLabWorldProbeHiddenKeys.has(selection.type === "sound"
    ? brainLabWorldProbeObjectKey("creature", selection.id)
    : selection.key);
}

function isBrainLabWorldProbeResourceVisible(resource) {
  return !brainLabWorldProbeHiddenKeys.has(brainLabWorldProbeObjectKey("resource", resource.id));
}

function isBrainLabWorldProbeEggVisible(egg) {
  return !brainLabWorldProbeHiddenKeys.has(brainLabWorldProbeObjectKey("egg", egg.id));
}

function isBrainLabWorldProbeSmallPreyVisible(prey) {
  return !brainLabWorldProbeHiddenKeys.has(brainLabWorldProbeObjectKey("smallPrey", prey.id));
}

function isBrainLabWorldProbeCreatureVisible(creature) {
  return !brainLabWorldProbeHiddenKeys.has(brainLabWorldProbeObjectKey("creature", creature.id));
}

function isBrainLabWorldProbeSoundVisible(creature) {
  return Number(creature.soundAmplitude || 0) > 0.05
    && !brainLabWorldProbeMutedSoundKeys.has(brainLabWorldProbeSoundKey(creature.id));
}

function brainLabWorldProbeObjectKey(type, id) {
  return `${type}:${id}`;
}

function brainLabWorldProbeSoundKey(id) {
  return `sound:${id}`;
}

function brainLabWorldProbeToggles() {
  const state = {
    plants: true,
    meatEggs: true,
    smallPrey: true,
    creatures: true,
    sound: true
  };
  if (!brainLabWorldProbe) {
    return state;
  }

  for (const control of brainLabWorldProbe.querySelectorAll("[data-brain-lab-world-toggle]")) {
    state[control.dataset.brainLabWorldToggle] = control.checked;
  }
  return state;
}

function resetBrainLabWorldProbeToggles() {
  if (!brainLabWorldProbe) {
    return;
  }

  for (const control of brainLabWorldProbe.querySelectorAll("[data-brain-lab-world-toggle]")) {
    control.checked = true;
  }
}

function applyBrainLabWorldProbeToggles() {
  renderBrainLabWorldProbe();
}

function setBrainLabWorldProbeFoodNeutral(inputs) {
  for (const input of inputs.filter(isBrainLabFoodInput)) {
    setBrainLabWorldProbeOverride(input.key, input.neutralValue, inputs);
  }
}

function setBrainLabWorldProbeOnlyPlants(inputs) {
  const plantDensity = findBrainLabInput("vision.plant_density", inputs)?.baselineValue ?? 0;
  const plantContact = findBrainLabInput("contact.plant_food", inputs)?.baselineValue ?? 0;
  for (const input of inputs.filter(isBrainLabMeatOrEggInput)) {
    setBrainLabWorldProbeOverride(input.key, input.neutralValue, inputs);
  }
  setBrainLabWorldProbeOverride("vision.food_density", plantDensity, inputs);
  setBrainLabWorldProbeOverride("contact.food", plantContact, inputs);
}

function setBrainLabWorldProbeOnlyMeatEggs(inputs) {
  const meatDensity = findBrainLabInput("vision.meat_density", inputs)?.baselineValue ?? 0;
  const meatContact = Math.max(
    findBrainLabInput("contact.meat_food", inputs)?.baselineValue ?? 0,
    findBrainLabInput("contact.egg_food", inputs)?.baselineValue ?? 0);
  for (const input of inputs.filter(isBrainLabPlantInput)) {
    setBrainLabWorldProbeOverride(input.key, input.neutralValue, inputs);
  }
  setBrainLabWorldProbeOverride("vision.food_density", meatDensity, inputs);
  setBrainLabWorldProbeOverride("contact.food", meatContact, inputs);
}

function applyBrainLabWorldProbeLocalEdits(inputs, toggles) {
  const ratios = brainLabWorldProbeVisibleRatios(toggles);
  if (toggles.plants && ratios.plants < 0.999) {
    scaleBrainLabWorldProbeInputs(inputs, isBrainLabPlantInput, ratios.plants);
  }
  if (toggles.meatEggs && ratios.meatEggs < 0.999) {
    scaleBrainLabWorldProbeInputs(inputs, isBrainLabMeatOrEggInput, ratios.meatEggs);
  }
  if ((toggles.plants || toggles.meatEggs || toggles.smallPrey) && ratios.food < 0.999) {
    scaleBrainLabWorldProbeInputs(inputs, isBrainLabWorldProbeGenericFoodInput, ratios.food);
  }
  if (toggles.creatures && ratios.creatures < 0.999) {
    scaleBrainLabWorldProbeInputs(inputs, isBrainLabWorldProbeCreatureSenseInput, ratios.creatures);
  }
  if (toggles.sound && ratios.sound < 0.999) {
    scaleBrainLabWorldProbeInputs(inputs, (input) => input.group === "Sound", ratios.sound);
  }
}

function brainLabWorldProbeVisibleRatios(toggles) {
  const scene = brainLabWorldProbeScene;
  if (!scene) {
    return {
      plants: 1,
      meatEggs: 1,
      smallPrey: 1,
      food: 1,
      creatures: 1,
      sound: 1
    };
  }

  const plants = (scene.resources || []).filter((resource) => resource.kind === "Plant");
  const meat = (scene.resources || []).filter((resource) => resource.kind === "Meat");
  const eggs = scene.eggs || [];
  const smallPrey = scene.smallPrey || [];
  const creatures = scene.creatures || [];
  const soundSources = creatures.filter((creature) => Number(creature.soundAmplitude || 0) > 0.05);
  const plantCounts = brainLabWorldProbeVisibleCount(plants, isBrainLabWorldProbeResourceVisible);
  const meatCounts = brainLabWorldProbeVisibleCount(meat, isBrainLabWorldProbeResourceVisible);
  const eggCounts = brainLabWorldProbeVisibleCount(eggs, isBrainLabWorldProbeEggVisible);
  const smallPreyCounts = brainLabWorldProbeVisibleCount(smallPrey, isBrainLabWorldProbeSmallPreyVisible);
  const creatureCounts = brainLabWorldProbeVisibleCount(creatures, isBrainLabWorldProbeCreatureVisible);
  const soundCounts = brainLabWorldProbeVisibleCount(soundSources, (creature) =>
    isBrainLabWorldProbeCreatureVisible(creature) && isBrainLabWorldProbeSoundVisible(creature));
  const enabledFoodTotal = (toggles.plants ? plantCounts.total : 0)
    + (toggles.meatEggs ? meatCounts.total + eggCounts.total : 0)
    + (toggles.smallPrey ? smallPreyCounts.total : 0);
  const enabledFoodVisible = (toggles.plants ? plantCounts.visible : 0)
    + (toggles.meatEggs ? meatCounts.visible + eggCounts.visible : 0)
    + (toggles.smallPrey ? smallPreyCounts.visible : 0);

  return {
    plants: brainLabWorldProbeRatio(plantCounts.visible, plantCounts.total),
    meatEggs: brainLabWorldProbeRatio(meatCounts.visible + eggCounts.visible, meatCounts.total + eggCounts.total),
    smallPrey: brainLabWorldProbeRatio(smallPreyCounts.visible, smallPreyCounts.total),
    food: brainLabWorldProbeRatio(enabledFoodVisible, enabledFoodTotal),
    creatures: brainLabWorldProbeRatio(creatureCounts.visible, creatureCounts.total),
    sound: brainLabWorldProbeRatio(soundCounts.visible, soundCounts.total)
  };
}

function brainLabWorldProbeVisibleCount(items, predicate) {
  return {
    total: items.length,
    visible: items.filter(predicate).length
  };
}

function brainLabWorldProbeRatio(visible, total) {
  return total > 0 ? Math.max(0, Math.min(1, visible / total)) : 1;
}

function scaleBrainLabWorldProbeInputs(inputs, predicate, ratio) {
  for (const input of inputs.filter(predicate)) {
    const baseline = Number(input.baselineValue);
    const neutral = Number(input.neutralValue);
    setBrainLabWorldProbeOverride(input.key, neutral + (baseline - neutral) * ratio, inputs);
  }
}

function isBrainLabWorldProbeGenericFoodInput(input) {
  return input.key === "vision.food_density" || input.key === "contact.food";
}

function isBrainLabWorldProbeCreatureSenseInput(input) {
  return input.key === "vision.creature_density"
    || input.key.includes(".creature_")
    || input.key.startsWith("scent.creature_similarity")
    || isBrainLabCreatureInput(input);
}

function setBrainLabWorldProbeOverride(key, value, inputs) {
  const input = findBrainLabInput(key, inputs);
  if (!input) {
    return;
  }

  brainLabWorldProbeOverrideKeys.add(key);
  const clamped = Math.min(Math.max(Number(value), Number(input.minimumValue)), Number(input.maximumValue));
  if (Math.abs(clamped - Number(input.baselineValue)) <= 0.0005) {
    delete brainLabOverrides[key];
  } else {
    brainLabOverrides[key] = clamped;
  }
}

function markBrainLabWorldProbeEdited(options = {}) {
  const { evaluate = true } = options;
  brainLabWorldProbeEdited = true;
  clearBrainLabPopulation();
  renderBrainLabWorldProbe();
  if (evaluate) {
    scheduleBrainLabEvaluate(80);
  }
}

function buildBrainLabWorldProbeEditPayload(force = false) {
  if (!brainLabWorldProbeScene || (!brainLabWorldProbeEdited && !force)) {
    return null;
  }

  const resources = (brainLabWorldProbeScene.resources || [])
    .filter(isBrainLabWorldProbeResourceVisible)
    .map((resource) => ({
      id: Number(resource.id),
      kind: resource.kind || "Plant",
      plantKind: resource.plantKind || "",
      x: Number(resource.x || 0),
      y: Number(resource.y || 0),
      radius: Number(resource.radius || 1),
      calories: Number(resource.calories || 0),
      maxCalories: Number(resource.maxCalories || resource.calories || 1),
      freshness: Number(resource.freshness ?? 1)
    }));
  const eggs = (brainLabWorldProbeScene.eggs || [])
    .filter(isBrainLabWorldProbeEggVisible)
    .map((egg) => ({
      id: Number(egg.id),
      generation: Number(egg.generation || 0),
      x: Number(egg.x || 0),
      y: Number(egg.y || 0),
      radius: Number(egg.radius || 1),
      energy: Number(egg.energy || 0),
      health: Number(egg.health || 1)
    }));
  const smallPrey = (brainLabWorldProbeScene.smallPrey || [])
    .filter(isBrainLabWorldProbeSmallPreyVisible)
    .map((prey) => ({
      id: Number(prey.id),
      x: Number(prey.x || 0),
      y: Number(prey.y || 0),
      radius: Number(prey.radius || 1),
      calories: Number(prey.calories || 0),
      maxCalories: Number(prey.maxCalories || prey.calories || 1),
      health: Number(prey.health || 0.2),
      maxHealth: Number(prey.maxHealth || prey.health || 0.2),
      headingRadians: Number(prey.headingRadians || 0),
      speed: Number(prey.speed || 0),
      grabPressure: Number(prey.grabPressure || 0)
    }));
  const creatures = (brainLabWorldProbeScene.creatures || [])
    .filter(isBrainLabWorldProbeCreatureVisible)
    .map((creature) => ({
      id: Number(creature.id),
      generation: Number(creature.generation || 0),
      brainArchitectureKind: creature.brainArchitectureKind || "",
      x: Number(creature.x || 0),
      y: Number(creature.y || 0),
      radius: Number(creature.radius || 1),
      headingRadians: Number(creature.headingRadians || 0),
      energyRatio: Number(creature.energyRatio ?? 1),
      healthRatio: Number(creature.healthRatio ?? 1),
      hunger: Number(creature.hunger || 0),
      soundAmplitude: brainLabWorldProbeMutedSoundKeys.has(brainLabWorldProbeSoundKey(creature.id))
        ? 0
        : Number(creature.soundAmplitude || 0),
      soundTone: Number(creature.soundTone || 0),
      isProbeSoundOnly: Boolean(creature.isProbeSoundOnly)
    }));

  return { resources, eggs, creatures, smallPrey };
}

function isBrainLabCreatureInput(input) {
  return input.key.includes("creature")
    || input.key.startsWith("contact.grab")
    || input.key === "contact.can_grab_creature"
    || input.key === "contact.is_holding_creature";
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
  const selectedClass = input.key === brainLabSelectedInputKey ? " is-selected" : "";
  return `
    <div class="brain-lab-input-row${overrideClass}${selectedClass}" data-brain-lab-input-row="${escapeHtml(input.key)}" title="${escapeHtml(input.meaning)}">
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

async function compareBrainLabPopulation() {
  const path = brainLabSelectedPath();
  if (!path || !brainLabEvaluation) {
    updateBrainLabButtons();
    return;
  }

  brainLabStatus.textContent = "Comparing population";
  const response = await fetch("/api/brain-lab/population-evaluate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      inputOverrides: brainLabOverrides,
      maxCreatures: 5000
    })
  });

  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Population comparison failed.");
    return;
  }

  brainLabPopulationEvaluation = await response.json();
  renderBrainLabPopulation();
  updateBrainLabButtons();
  brainLabStatus.textContent = [
    `${formatNumber(brainLabPopulationEvaluation.evaluatedCreatureCount)} creatures compared`,
    `${formatPercent(brainLabPopulationEvaluation.changedCreatureShare)} changed`,
    `${formatPercent(brainLabPopulationEvaluation.gateFlipCreatureShare)} gate flips`
  ].join(" | ");
}

function renderBrainLabPopulation() {
  if (!brainLabPopulation) {
    return;
  }

  if (!brainLabPopulationEvaluation) {
    brainLabPopulation.textContent = "No population comparison yet.";
    return;
  }

  const outputs = [...(brainLabPopulationEvaluation.outputs || [])]
    .sort((left, right) => Number(right.meanAbsoluteDelta || 0) - Number(left.meanAbsoluteDelta || 0));
  brainLabPopulation.innerHTML = `
    <div class="brain-lab-population-summary">
      <strong>${formatNumber(brainLabPopulationEvaluation.evaluatedCreatureCount)} compared</strong>
      <span>${formatNumber(brainLabPopulationEvaluation.changedCreatureCount)} changed (${formatPercent(brainLabPopulationEvaluation.changedCreatureShare)})</span>
      <span>${formatNumber(brainLabPopulationEvaluation.gateFlipCreatureCount)} gate-flipped (${formatPercent(brainLabPopulationEvaluation.gateFlipCreatureShare)})</span>
      ${brainLabPopulationEvaluation.skippedCreatureCount ? `<span>${formatNumber(brainLabPopulationEvaluation.skippedCreatureCount)} skipped</span>` : ""}
      ${brainLabPopulationEvaluation.supportsRawInputOverrides === false ? `<span>some brains did not support raw overrides</span>` : ""}
    </div>
    <table class="brain-lab-population-table">
      <thead>
        <tr>
          <th>Output</th>
          <th>Mean</th>
          <th>Mean Delta</th>
          <th>Mean Abs</th>
          <th>Changed</th>
          <th>Gate Flips</th>
        </tr>
      </thead>
      <tbody>
        ${outputs.map(renderBrainLabPopulationRow).join("")}
      </tbody>
    </table>
  `;
}

function renderBrainLabPopulationRow(output) {
  const deltaClass = output.meanDelta > 0 ? "is-positive" : output.meanDelta < 0 ? "is-negative" : "";
  return `
    <tr class="${Number(output.changedCreatureCount || 0) > 0 ? "is-changed" : ""}">
      <td><strong>${escapeHtml(output.name)}</strong><code>${escapeHtml(output.key)}</code></td>
      <td>${formatBrainLabNumber(output.baselineMean)} -> ${formatBrainLabNumber(output.modifiedMean)}</td>
      <td class="${deltaClass}">${formatBrainLabDelta(output.meanDelta)}</td>
      <td>${formatBrainLabNumber(output.meanAbsoluteDelta)}</td>
      <td>${formatNumber(output.changedCreatureCount)} (${formatPercent(output.changedCreatureShare)})</td>
      <td>${formatNumber(output.gateFlipCount)} (${formatPercent(output.gateFlipShare)})</td>
    </tr>
  `;
}

async function runBrainLabPresetMatrix() {
  const path = brainLabSelectedPath();
  if (!path || !brainLabSnapshot) {
    updateBrainLabButtons();
    return;
  }

  brainLabStatus.textContent = "Running preset matrix";
  const response = await fetch("/api/brain-lab/preset-matrix", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      maxCreatures: 5000
    })
  });

  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Preset matrix failed.");
    return;
  }

  brainLabPresetMatrixResult = await response.json();
  renderBrainLabPresetMatrix();
  updateBrainLabButtons();
  const strongest = [...(brainLabPresetMatrixResult.rows || [])]
    .sort((left, right) => Number(right.changedCreatureShare || 0) - Number(left.changedCreatureShare || 0))[0];
  brainLabStatus.textContent = strongest
    ? `Preset matrix complete | strongest ${strongest.name} (${formatPercent(strongest.changedCreatureShare)} changed)`
    : "Preset matrix complete";
}

function renderBrainLabPresetMatrix() {
  if (!brainLabPresetMatrix) {
    return;
  }

  if (!brainLabPresetMatrixResult) {
    brainLabPresetMatrix.textContent = "No preset matrix yet.";
    return;
  }

  const rows = [...(brainLabPresetMatrixResult.rows || [])]
    .sort((left, right) => Number(right.changedCreatureShare || 0) - Number(left.changedCreatureShare || 0));
  brainLabPresetMatrix.innerHTML = `
    <div class="brain-lab-preset-matrix-summary">
      <strong>${formatNumber(brainLabPresetMatrixResult.totalCreatureCount)} creatures</strong>
      <span>cap ${formatNumber(brainLabPresetMatrixResult.maxCreatures)}</span>
      <span>${escapeHtml(brainLabPresetMatrixResult.snapshotPath)}</span>
    </div>
    <table class="brain-lab-preset-matrix-table">
      <thead>
        <tr>
          <th>Preset</th>
          <th>Changed</th>
          <th>Gate Flips</th>
          <th>Max Delta</th>
          <th>Top Outputs</th>
        </tr>
      </thead>
      <tbody>
        ${rows.map(renderBrainLabPresetMatrixRow).join("")}
      </tbody>
    </table>
  `;
}

function renderBrainLabPresetMatrixRow(row) {
  const topOutputs = (row.topOutputs || [])
    .map((output) => `${escapeHtml(output.name)} ${formatBrainLabNumber(output.meanAbsoluteDelta)}`)
    .join("<br>");
  return `
    <tr>
      <td><strong>${escapeHtml(row.name)}</strong><code>${escapeHtml(row.key)}</code></td>
      <td>${formatNumber(row.changedCreatureCount)} (${formatPercent(row.changedCreatureShare)})</td>
      <td>${formatNumber(row.gateFlipCreatureCount)} (${formatPercent(row.gateFlipCreatureShare)})</td>
      <td>${formatBrainLabNumber(row.maxAbsoluteOutputDelta)}</td>
      <td>${topOutputs}</td>
    </tr>
  `;
}

async function compareBrainLabProfiles() {
  const path = brainLabSelectedPath();
  if (!path || !brainLabSnapshot) {
    updateBrainLabButtons();
    return;
  }

  if (brainLabProfileComparisonRunning) {
    return;
  }

  brainLabProfileComparisonRunning = true;
  brainLabProfileComparisonCohortKey = null;
  updateBrainLabButtons();
  const inputOverrides = { ...brainLabOverrides };
  const worldProbeEnvironment = buildBrainLabWorldProbeEnvironmentPayload();
  try {
    if (brainLabProfileScopeSelect?.value === "all") {
      await compareAllBrainLabProfiles(path, inputOverrides, worldProbeEnvironment);
    } else {
      await compareSampleBrainLabProfiles(path, inputOverrides, worldProbeEnvironment);
    }
  } finally {
    brainLabProfileComparisonRunning = false;
    if (brainLabProfileComparisonResult) {
      brainLabProfileComparisonResult.isRunning = false;
      renderBrainLabProfileComparison();
    }

    updateBrainLabButtons();
  }
}

async function compareSampleBrainLabProfiles(path, inputOverrides, worldProbeEnvironment) {
  brainLabStatus.textContent = "Comparing behavior profile sample";
  let offset = 0;
  let template = null;
  let totalCreatureCount = Number(brainLabSnapshot?.creatureCount ?? brainLabProfileComparisonSampleSize);
  const initialSampleTotal = Math.min(brainLabProfileComparisonSampleSize, totalCreatureCount);
  const rows = [];
  brainLabProfileComparisonResult = buildBrainLabProfileComparisonSampleResult(
    { snapshotPath: path, totalCreatureCount },
    rows,
    totalCreatureCount,
    initialSampleTotal,
    true,
    `Loading snapshot and evaluating first ${formatNumber(Math.min(brainLabProfileComparisonSampleBatchSize, initialSampleTotal))} creatures`);
  renderBrainLabProfileComparison();

  while (rows.length < brainLabProfileComparisonSampleSize) {
    const batchSize = Math.min(
      brainLabProfileComparisonSampleBatchSize,
      brainLabProfileComparisonSampleSize - rows.length);
    const result = await fetchBrainLabProfileComparisonBatch(
      path,
      inputOverrides,
      worldProbeEnvironment,
      offset,
      batchSize);
    template ??= result;
    totalCreatureCount = Number(result.totalCreatureCount || totalCreatureCount);
    const sampleTotal = Math.min(brainLabProfileComparisonSampleSize, totalCreatureCount);
    const batchRows = result.rows || [];
    rows.push(...batchRows);
    offset += batchRows.length;

    brainLabProfileComparisonResult = buildBrainLabProfileComparisonSampleResult(
      template,
      rows,
      totalCreatureCount,
      sampleTotal,
      true);
    renderBrainLabProfileComparison();
    brainLabStatus.textContent = [
      "Comparing behavior profile sample",
      `${formatNumber(rows.length)} / ${formatNumber(sampleTotal)} creatures`,
      `${formatPercent(rows.length / Math.max(1, sampleTotal))}`
    ].join(" | ");

    if (batchRows.length === 0 || rows.length >= sampleTotal) {
      break;
    }

    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  if (template) {
    brainLabProfileComparisonResult = buildBrainLabProfileComparisonSampleResult(
      template,
      rows,
      totalCreatureCount,
      Math.min(brainLabProfileComparisonSampleSize, totalCreatureCount),
      false);
    renderBrainLabProfileComparison();
  }

  brainLabStatus.textContent = [
    "Profile comparison complete",
    `${formatNumber(brainLabProfileComparisonResult.evaluatedCreatureCount)} creatures`,
    `${formatNumber(brainLabProfileComparisonResult.evaluatedFixtureCount)} setups`
  ].join(" | ");
}

async function compareAllBrainLabProfiles(path, inputOverrides, worldProbeEnvironment) {
  brainLabStatus.textContent = "Comparing all behavior profiles";
  let offset = 0;
  let template = null;
  const rows = [];
  brainLabProfileComparisonResult = {
    snapshotPath: path,
    totalCreatureCount: 0,
    creatureOffset: 0,
    maxCreatures: 0,
    evaluatedCreatureCount: 0,
    skippedCreatureCount: 0,
    totalFixtureCount: 0,
    evaluatedFixtureCount: 0,
    skippedFixtureCount: 0,
    cohorts: [],
    rows: [],
    profileScope: "all",
    isRunning: true,
    progressCreatureCount: 0,
    progressTotalCreatureCount: 1,
    progressLabel: `Loading snapshot and evaluating first ${formatNumber(brainLabProfileComparisonBatchSize)} creatures`
  };
  renderBrainLabProfileComparison();

  while (true) {
    const result = await fetchBrainLabProfileComparisonBatch(
      path,
      inputOverrides,
      worldProbeEnvironment,
      offset,
      brainLabProfileComparisonBatchSize);
    template ??= result;
    const batchRows = result.rows || [];
    rows.push(...batchRows);
    offset += batchRows.length;

    brainLabProfileComparisonResult = buildBrainLabProfileComparisonResult(
      template,
      rows,
      result.totalCreatureCount,
      true);
    renderBrainLabProfileComparison();
    brainLabStatus.textContent = [
      "Comparing all behavior profiles",
      `${formatNumber(rows.length)} / ${formatNumber(result.totalCreatureCount)} creatures`,
      `${formatPercent(rows.length / Math.max(1, result.totalCreatureCount))}`
    ].join(" | ");

    if (batchRows.length === 0 || rows.length >= result.totalCreatureCount) {
      break;
    }

    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  if (template) {
    brainLabProfileComparisonResult = buildBrainLabProfileComparisonResult(
      template,
      rows,
      template.totalCreatureCount,
      false);
    renderBrainLabProfileComparison();
  }

  brainLabStatus.textContent = [
    "Profile comparison complete",
    `${formatNumber(rows.length)} creatures`,
    `${formatNumber(template?.evaluatedFixtureCount || 0)} setups`
  ].join(" | ");
}

async function fetchBrainLabProfileComparisonBatch(
  path,
  inputOverrides,
  worldProbeEnvironment,
  creatureOffset,
  maxCreatures) {
  const response = await fetch("/api/brain-lab/profile-comparison", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      inputOverrides,
      worldProbeEnvironment,
      creatureOffset,
      maxCreatures,
      maxFixtures: 100
    })
  });

  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Behavior profile comparison failed.");
    throw new Error(brainLabStatus.textContent);
  }

  return response.json();
}

function buildBrainLabProfileComparisonResult(template, rows, totalCreatureCount, isRunning) {
  const total = Number(totalCreatureCount || template?.totalCreatureCount || rows.length);
  return {
    snapshotPath: template?.snapshotPath || brainLabSelectedPath(),
    totalCreatureCount: total,
    creatureOffset: 0,
    maxCreatures: total,
    evaluatedCreatureCount: rows.length,
    skippedCreatureCount: Math.max(0, total - rows.length),
    totalFixtureCount: template?.totalFixtureCount || 0,
    evaluatedFixtureCount: template?.evaluatedFixtureCount || 0,
    skippedFixtureCount: template?.skippedFixtureCount || 0,
    cohorts: buildBrainLabProfileComparisonCohorts(rows),
    rows: [...rows],
    profileScope: "all",
    isRunning,
    progressCreatureCount: rows.length,
    progressTotalCreatureCount: total,
    progressLabel: `${formatNumber(rows.length)} / ${formatNumber(total)} creatures`
  };
}

function buildBrainLabProfileComparisonSampleResult(
  template,
  rows,
  totalCreatureCount,
  sampleLimit,
  isRunning,
  progressLabel = null) {
  const total = Number(totalCreatureCount || template?.totalCreatureCount || rows.length);
  const cap = Math.min(Number(sampleLimit || brainLabProfileComparisonSampleSize), Math.max(1, total || sampleLimit || rows.length || 1));
  return {
    snapshotPath: template?.snapshotPath || brainLabSelectedPath(),
    totalCreatureCount: total,
    creatureOffset: 0,
    maxCreatures: cap,
    evaluatedCreatureCount: rows.length,
    skippedCreatureCount: Math.max(0, (isRunning ? cap : total) - rows.length),
    totalFixtureCount: template?.totalFixtureCount || 0,
    evaluatedFixtureCount: template?.evaluatedFixtureCount || 0,
    skippedFixtureCount: template?.skippedFixtureCount || 0,
    cohorts: buildBrainLabProfileComparisonCohorts(rows),
    rows: [...rows],
    profileScope: "sample",
    isRunning,
    progressCreatureCount: rows.length,
    progressTotalCreatureCount: cap,
    progressLabel: progressLabel || `${formatNumber(rows.length)} / ${formatNumber(cap)} sample creatures`
  };
}

function buildBrainLabProfileComparisonCohorts(rows) {
  const groups = new Map();
  for (const row of rows) {
    const key = row.cohortKey || "weak-mixed-no-clear-signal";
    if (!groups.has(key)) {
      groups.set(key, []);
    }

    groups.get(key).push(row);
  }

  return [...groups.entries()]
    .map(([key, groupRows]) => {
      const orderedRows = [...groupRows].sort((left, right) =>
        Number(right.generation || 0) - Number(left.generation || 0)
        || Number(left.creatureId || 0) - Number(right.creatureId || 0));
      const traits = countBrainLabProfileCohortValues(orderedRows.flatMap((row) => row.cohortTraits || []));
      const fingerprints = countBrainLabProfileCohortValues(orderedRows.flatMap((row) => row.cohortFingerprints || []));
      return {
        key,
        name: brainLabProfileCohortName(traits, fingerprints),
        summary: brainLabProfileCohortSummary(orderedRows.length, traits, fingerprints),
        creatureCount: orderedRows.length,
        representativeCreatureId: orderedRows[0]?.creatureId || 0,
        creatureIds: orderedRows.map((row) => row.creatureId),
        traits,
        fingerprints
      };
    })
    .sort((left, right) =>
      Number(right.creatureCount || 0) - Number(left.creatureCount || 0)
      || String(left.name || "").localeCompare(String(right.name || "")));
}

function countBrainLabProfileCohortValues(values) {
  const counts = new Map();
  for (const value of values) {
    if (!value) {
      continue;
    }

    counts.set(value, (counts.get(value) || 0) + 1);
  }

  return [...counts.entries()]
    .sort((left, right) => right[1] - left[1] || left[0].localeCompare(right[0]))
    .slice(0, 6)
    .map(([value, count]) => `${value} (${formatNumber(count)})`);
}

function brainLabProfileCohortName(traits, fingerprints) {
  if (traits.length > 0) {
    return traits.slice(0, 3).map(brainLabProfileCohortNamePart).join(" / ");
  }

  if (fingerprints.length > 0) {
    return `Fingerprint: ${fingerprints.slice(0, 3).map(brainLabProfileCohortNamePart).join(" / ")}`;
  }

  return "Weak/Mixed: no clear signal";
}

function brainLabProfileCohortSummary(creatureCount, traits, fingerprints) {
  if (traits.length > 0) {
    return `${formatNumber(creatureCount)} creatures matching ${traits.slice(0, 4).join(", ")}`;
  }

  if (fingerprints.length > 0) {
    return `${formatNumber(creatureCount)} creatures matching ${fingerprints.slice(0, 4).join(", ")}`;
  }

  return `${formatNumber(creatureCount)} creatures with no strong profile signals or fingerprints`;
}

function brainLabProfileCohortNamePart(value) {
  const index = String(value || "").indexOf(" (");
  return index > 0 ? value.slice(0, index) : value;
}

function renderBrainLabProfileComparison() {
  if (!brainLabProfileComparison) {
    return;
  }

  if (!brainLabProfileComparisonResult) {
    brainLabProfileComparison.textContent = "No behavior profile comparison yet.";
    return;
  }

  const cohorts = brainLabProfileComparisonResult.cohorts || [];
  const selectedCohort = cohorts.find((cohort) => cohort.key === brainLabProfileComparisonCohortKey) || null;
  const selectedCreatureIds = selectedCohort
    ? new Set((selectedCohort.creatureIds || []).map((id) => Number(id)))
    : null;
  const rows = (brainLabProfileComparisonResult.rows || [])
    .filter((row) => !selectedCreatureIds || selectedCreatureIds.has(Number(row.creatureId)));
  const cohortCards = renderBrainLabProfileCohorts(cohorts, selectedCohort);
  const scopeLabel = brainLabProfileComparisonResult.profileScope === "all"
    ? "all creatures"
    : `cap ${formatNumber(brainLabProfileComparisonResult.maxCreatures)}`;
  brainLabProfileComparison.innerHTML = `
    <div class="brain-lab-profile-comparison-summary">
      <strong>${formatNumber(brainLabProfileComparisonResult.evaluatedCreatureCount)} profiled</strong>
      <span>${formatNumber(brainLabProfileComparisonResult.totalCreatureCount)} total creatures</span>
      <span>${scopeLabel}</span>
      <span>${formatNumber(brainLabProfileComparisonResult.evaluatedFixtureCount)} setups</span>
      ${selectedCohort ? `<span>showing ${formatNumber(rows.length)} in ${escapeHtml(selectedCohort.name)}</span>` : ""}
      ${brainLabProfileComparisonResult.skippedCreatureCount ? `<span>${formatNumber(brainLabProfileComparisonResult.skippedCreatureCount)} creatures ${brainLabProfileComparisonResult.isRunning ? "remaining" : "skipped by cap"}</span>` : ""}
      ${brainLabProfileComparisonResult.skippedFixtureCount ? `<span>${formatNumber(brainLabProfileComparisonResult.skippedFixtureCount)} setups skipped by cap</span>` : ""}
    </div>
    ${renderBrainLabProfileComparisonProgress(brainLabProfileComparisonResult)}
    ${cohortCards}
    ${renderBrainLabProfileCohortDetail(selectedCohort, rows)}
    <table class="brain-lab-profile-comparison-table">
      <thead>
        <tr>
          <th>Creature</th>
          <th>Brain</th>
          <th>Food</th>
          <th>Scent</th>
          <th>Sound</th>
          <th>Creature</th>
          <th>Conflict</th>
          <th>Idle</th>
          <th>Fingerprints</th>
        </tr>
      </thead>
      <tbody>
        ${rows.map(renderBrainLabProfileComparisonRow).join("")}
      </tbody>
    </table>
  `;
}

function renderBrainLabProfileComparisonProgress(result) {
  if (!result?.isRunning) {
    return "";
  }

  const total = Math.max(1, Number(result.progressTotalCreatureCount || result.totalCreatureCount || 0));
  const progressValue = result.progressCreatureCount ?? result.evaluatedCreatureCount;
  const value = Math.max(0, Math.min(total, Number(progressValue || 0)));
  const label = result.progressLabel || `${formatNumber(value)} / ${formatNumber(total)} creatures`;
  return `
    <div class="brain-lab-profile-progress">
      <progress max="${total}" value="${value}"></progress>
      <span>${escapeHtml(label)}</span>
    </div>
  `;
}

function renderBrainLabProfileCohorts(cohorts, selectedCohort) {
  if (!cohorts?.length) {
    return "";
  }

  const allActive = !selectedCohort ? " is-active" : "";
  return `
    <div class="brain-lab-profile-cohorts">
      <button class="brain-lab-profile-cohort${allActive}" type="button" data-brain-lab-profile-cohort="__all">
        <strong>All Profiles</strong>
        <span>${formatNumber(brainLabProfileComparisonResult.evaluatedCreatureCount)} creatures</span>
      </button>
      ${cohorts.map((cohort) => renderBrainLabProfileCohort(cohort, selectedCohort?.key === cohort.key)).join("")}
    </div>
  `;
}

function renderBrainLabProfileCohort(cohort, selected) {
  const traits = (cohort.traits || []).slice(0, 4).join(" | ");
  const fingerprints = (cohort.fingerprints || []).slice(0, 4).join(" | ");
  const title = [cohort.summary, traits, fingerprints].filter(Boolean).join(" | ");
  return `
    <button class="brain-lab-profile-cohort${selected ? " is-active" : ""}" type="button" data-brain-lab-profile-cohort="${escapeHtml(cohort.key || "")}" title="${escapeHtml(title)}">
      <strong>${escapeHtml(cohort.name || "Cohort")}</strong>
      <span>${formatNumber(cohort.creatureCount)} creatures</span>
      <code>rep #${formatNumber(cohort.representativeCreatureId)}</code>
      ${traits ? `<small>${escapeHtml(traits)}</small>` : ""}
      ${fingerprints ? `<small>${escapeHtml(fingerprints)}</small>` : ""}
    </button>
  `;
}

function renderBrainLabProfileCohortDetail(cohort, rows) {
  if (!cohort) {
    return "";
  }

  const representative = brainLabProfileComparisonCreature(cohort.representativeCreatureId) || rows[0] || null;
  const interpretation = renderBrainLabProfileCohortInterpretation(cohort);
  const evidence = renderBrainLabProfileCohortEvidence(rows);
  const traits = (cohort.traits || [])
    .slice(0, 8)
    .map((trait) => `<span>${escapeHtml(trait)}</span>`)
    .join("");
  const fingerprints = (cohort.fingerprints || [])
    .slice(0, 8)
    .map((fingerprint) => `<span>${escapeHtml(fingerprint)}</span>`)
    .join("");
  const representativeLabel = representative
    ? `#${formatNumber(representative.creatureId)} gen ${formatNumber(representative.generation)} ${escapeHtml(representative.brainArchitectureKind || "")}`
    : `#${formatNumber(cohort.representativeCreatureId)}`;

  return `
    <div class="brain-lab-profile-cohort-detail">
      <div class="brain-lab-profile-cohort-detail-heading">
        <div>
          <h4>${escapeHtml(cohort.name || "Selected Cohort")}</h4>
          <span>${formatNumber(cohort.creatureCount)} creatures | representative ${representativeLabel}</span>
        </div>
        <div class="brain-lab-actions">
          <button class="secondary" type="button" data-brain-lab-profile-creature="${escapeHtml(cohort.representativeCreatureId)}">Load Rep</button>
          <button class="secondary" type="button" data-brain-lab-profile-action="export-species" data-brain-lab-profile-action-creature="${escapeHtml(cohort.representativeCreatureId)}">Export Rep Species</button>
          <button class="secondary" type="button" data-brain-lab-profile-action="export-brain" data-brain-lab-profile-action-creature="${escapeHtml(cohort.representativeCreatureId)}">Export Rep Brain</button>
        </div>
      </div>
      <div class="brain-lab-profile-cohort-detail-grid">
        <div>
          <strong>Meaning</strong>
          ${interpretation}
        </div>
        <div>
          <strong>Shared Signals</strong>
          <div class="brain-lab-profile-cohort-detail-tags">${traits || `<span>No strong profile traits.</span>`}</div>
        </div>
        <div>
          <strong>Fingerprints</strong>
          <div class="brain-lab-profile-cohort-detail-tags">${fingerprints || `<span>No strong fingerprints.</span>`}</div>
        </div>
        <div>
          <strong>Probe Evidence</strong>
          ${evidence}
        </div>
      </div>
    </div>
  `;
}

function renderBrainLabProfileCohortInterpretation(cohort) {
  const traits = cohort.traits || [];
  const explanations = traits
    .slice(0, 8)
    .map(brainLabProfileTraitMeaning)
    .filter(Boolean);
  const hasConflict = traits.some((trait) => String(trait).startsWith("Conflict:"));
  const conflictNote = hasConflict
    ? `<p><strong>Conflict</strong> means a compound probe setup where cues compete, such as food plus creature or food plus sound. It does not mean combat by itself.</p>`
    : "";

  if (explanations.length === 0) {
    return `
      <p>This cohort has weak or mixed profile signals. The creatures grouped together because the suite did not find a stronger shared food, sound, creature, conflict, scent, or idle pattern.</p>
      ${conflictNote}
    `;
  }

  return `
    ${conflictNote}
    <ul>
      ${explanations.map((explanation) => `<li>${escapeHtml(explanation)}</li>`).join("")}
    </ul>
  `;
}

function brainLabProfileTraitMeaning(rawTrait) {
  const trait = brainLabProfileCohortNamePart(rawTrait || "");
  const separator = trait.indexOf(":");
  if (separator < 0) {
    return trait;
  }

  const section = trait.slice(0, separator).trim();
  const value = trait.slice(separator + 1).trim();
  if (!value) {
    return trait;
  }

  switch (section) {
    case "Food":
      return `Food: strongest pull is ${value}.`;
    case "Scent":
      return `Scent: strongest scent pattern is ${value}.`;
    case "Sound":
      return `Sound: strongest sound pattern is ${value}.`;
    case "Creature":
      return `Creature: response to another creature is ${value}.`;
    case "Conflict":
      return `Conflict: when cues compete, ${value} tends to dominate.`;
    case "Idle":
      return `Idle: with no clear cue, tendency is ${value}.`;
    default:
      return `${section}: ${value}.`;
  }
}

function renderBrainLabProfileCohortEvidence(rows) {
  const evidence = aggregateBrainLabProfileCohortEvidence(rows);
  if (evidence.length === 0) {
    return `<p class="brain-lab-muted">No probe evidence was attached to these profile sections.</p>`;
  }

  return `
    <div class="brain-lab-profile-cohort-evidence">
      ${evidence.map((section) => `
        <div>
          <span>${escapeHtml(section.name)}</span>
          <code>${section.items.map((item) => `${escapeHtml(item.name)} (${formatNumber(item.count)})`).join(" | ")}</code>
        </div>
      `).join("")}
    </div>
  `;
}

function aggregateBrainLabProfileCohortEvidence(rows) {
  const order = ["Food", "Scent", "Sound", "Creature", "Conflict", "Idle"];
  const sections = new Map();
  for (const row of rows || []) {
    for (const section of row.profile?.sections || []) {
      const name = section.name || section.key || "Profile";
      if (!sections.has(name)) {
        sections.set(name, new Map());
      }

      const counts = sections.get(name);
      for (const item of section.evidence || []) {
        counts.set(item, (counts.get(item) || 0) + 1);
      }
    }
  }

  return [...sections.entries()]
    .map(([name, counts]) => ({
      name,
      items: [...counts.entries()]
        .sort((left, right) => right[1] - left[1] || left[0].localeCompare(right[0]))
        .slice(0, 5)
        .map(([itemName, count]) => ({ name: itemName, count }))
    }))
    .filter((section) => section.items.length > 0)
    .sort((left, right) => {
      const leftIndex = order.indexOf(left.name);
      const rightIndex = order.indexOf(right.name);
      return (leftIndex < 0 ? 99 : leftIndex) - (rightIndex < 0 ? 99 : rightIndex)
        || left.name.localeCompare(right.name);
    });
}

function brainLabProfileComparisonCreature(creatureId) {
  const id = Number(creatureId || 0);
  if (!id) {
    return null;
  }

  return (brainLabProfileComparisonResult?.rows || [])
    .find((row) => Number(row.creatureId) === id) ?? null;
}

function renderBrainLabProfileComparisonRow(row) {
  const fingerprints = (row.fingerprints || [])
    .slice(0, 4)
    .map(renderBrainLabBehaviorFingerprint)
    .join("");
  return `
    <tr>
      <td>
        <strong>#${formatNumber(row.creatureId)}</strong>
        <code>gen ${formatNumber(row.generation)}</code>
        <button class="secondary brain-lab-profile-load-creature" type="button" data-brain-lab-profile-creature="${escapeHtml(row.creatureId)}">Load</button>
      </td>
      <td>
        <strong>${escapeHtml(row.brainArchitectureKind || "unknown")}</strong>
        <code>brain ${formatNumber(row.brainId)} genome ${formatNumber(row.genomeId)}</code>
      </td>
      ${renderBrainLabProfileComparisonCell(row.profile, "food")}
      ${renderBrainLabProfileComparisonCell(row.profile, "scent")}
      ${renderBrainLabProfileComparisonCell(row.profile, "sound")}
      ${renderBrainLabProfileComparisonCell(row.profile, "creature")}
      ${renderBrainLabProfileComparisonCell(row.profile, "conflict")}
      ${renderBrainLabProfileComparisonCell(row.profile, "idle")}
      <td><div class="brain-lab-behavior-labels">${fingerprints || `<span class="brain-lab-muted">none</span>`}</div></td>
    </tr>
  `;
}

function renderBrainLabProfileComparisonCell(profile, key) {
  const section = (profile?.sections || []).find((candidate) => candidate.key === key);
  if (!section) {
    return `<td><span class="brain-lab-muted">none</span></td>`;
  }

  const traits = (section.traits || []).slice(0, 3).join(" | ");
  const evidence = (section.evidence || []).length > 0
    ? `Evidence: ${section.evidence.join(", ")}`
    : "No direct evidence rows.";
  const title = [traits, evidence].filter(Boolean).join(" | ");
  return `
    <td title="${escapeHtml(title)}">
      <strong>${escapeHtml(section.summary || "No clear signal.")}</strong>
      ${traits ? `<code>${escapeHtml(traits)}</code>` : ""}
    </td>
  `;
}

function selectBrainLabProfileCohort(key) {
  brainLabProfileComparisonCohortKey = key || null;
  renderBrainLabProfileComparison();
}

async function loadBrainLabProfileCreature(creatureId) {
  if (!brainLabSnapshot || !brainLabCreatureSelect) {
    return;
  }

  const value = String(creatureId);
  if (![...brainLabCreatureSelect.options].some((option) => option.value === value)) {
    const row = brainLabProfileComparisonCreature(creatureId);
    if (!row) {
      brainLabStatus.textContent = `Creature #${formatNumber(creatureId)} is outside the loaded snapshot creature list.`;
      return;
    }

    const option = document.createElement("option");
    option.value = value;
    option.textContent = `#${formatNumber(row.creatureId)} gen ${formatNumber(row.generation)} ${row.brainArchitectureKind}`;
    option.title = "Added from behavior profile comparison.";
    brainLabCreatureSelect.append(option);
    brainLabCreatureSelect.disabled = false;
  }

  brainLabCreatureSelect.value = value;
  brainLabStatus.textContent = `Loading creature #${formatNumber(creatureId)}`;
  await loadBrainLabWorldProbe();
  await evaluateBrainLab();
}

async function saveBrainLabSpeciesProfile(creatureOverride = null) {
  const path = brainLabSelectedPath();
  const creature = creatureOverride || selectedBrainLabCreature();
  if (!path || !creature) {
    brainLabStatus.textContent = "Load a snapshot and choose a creature before exporting.";
    return;
  }

  const creatureId = Number(creature.id ?? creature.creatureId);
  const defaultName = `${brainLabSnapshot?.scenarioName || "Brain Lab"} #${creatureId} gen ${creature.generation}`;
  const name = prompt("Species profile name", defaultName);
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    brainLabStatus.textContent = "Species profile name is required.";
    return;
  }

  const notes = prompt(
    "Species notes",
    `Saved from Brain Lab snapshot ${path}, creature #${creatureId} gen ${creature.generation}.`);
  if (notes === null) {
    return;
  }

  const exportPairedBrain = confirm("Also save this creature's brain as a paired catalog brain and make it the species default?");
  brainLabStatus.textContent = "Saving species profile";
  const response = await fetch("/api/brain-lab/species-exports", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      name: trimmedName,
      notes: notes.trim(),
      creatureId,
      exportPairedBrain
    })
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Brain Lab species export failed." }));
    brainLabStatus.textContent = problem.error || "Brain Lab species export failed.";
    return;
  }

  const result = await response.json();
  await loadSpeciesCatalog(result.species.path);
  if (result.brain) {
    await loadBrainCatalog(result.brain.path);
  }

  brainLabStatus.textContent = result.brain
    ? `Saved species profile ${result.species.name} with paired brain ${result.brain.name}.`
    : `Saved species profile ${result.species.name}.`;
}

async function saveBrainLabBrainProfile(creatureOverride = null) {
  const path = brainLabSelectedPath();
  const creature = creatureOverride || selectedBrainLabCreature();
  if (!path || !creature) {
    brainLabStatus.textContent = "Load a snapshot and choose a creature before exporting.";
    return;
  }

  const creatureId = Number(creature.id ?? creature.creatureId);
  const defaultName = `${brainLabSnapshot?.scenarioName || "Brain Lab"} #${creatureId} brain`;
  const name = prompt("Brain profile name", defaultName);
  if (name === null) {
    return;
  }

  const trimmedName = name.trim();
  if (!trimmedName) {
    brainLabStatus.textContent = "Brain profile name is required.";
    return;
  }

  const notes = prompt(
    "Brain notes",
    `Saved from Brain Lab snapshot ${path}, creature #${creatureId} gen ${creature.generation}.`);
  if (notes === null) {
    return;
  }

  brainLabStatus.textContent = "Saving brain profile";
  const response = await fetch("/api/brain-lab/brain-exports", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      name: trimmedName,
      notes: notes.trim(),
      creatureId
    })
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ error: "Brain Lab brain export failed." }));
    brainLabStatus.textContent = problem.error || "Brain Lab brain export failed.";
    return;
  }

  const result = await response.json();
  await loadBrainCatalog(result.brain.path);
  brainLabStatus.textContent = `Saved brain profile ${result.brain.name}.`;
}

async function runBrainLabProbeTests() {
  const path = brainLabSelectedPath();
  const creatureId = Number(brainLabCreatureSelect?.value || 0);
  if (!path || !creatureId) {
    updateBrainLabButtons();
    return;
  }

  brainLabStatus.textContent = "Running probe tests";
  const response = await fetch("/api/brain-lab/probe-tests", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      snapshotPath: path,
      creatureId,
      inputOverrides: brainLabOverrides,
      worldProbeEnvironment: buildBrainLabWorldProbeEnvironmentPayload(),
      maxFixtures: 100
    })
  });

  if (!response.ok) {
    brainLabStatus.textContent = await responseErrorMessage(response, "Probe tests failed.");
    return;
  }

  brainLabProbeTestResult = await response.json();
  renderBrainLabProbeTests();
  updateBrainLabButtons();
  const strongest = [...(brainLabProbeTestResult.rows || [])]
    .sort((left, right) => Number(right.maxAbsoluteOutputDelta || 0) - Number(left.maxAbsoluteOutputDelta || 0))[0];
  brainLabStatus.textContent = strongest
    ? `Probe tests complete | strongest ${strongest.name} (${formatBrainLabNumber(strongest.maxAbsoluteOutputDelta)} max delta)`
    : "Probe tests complete";
}

function renderBrainLabProbeTests() {
  if (!brainLabProbeTests) {
    return;
  }

  if (!brainLabProbeTestResult) {
    brainLabProbeTests.textContent = "No probe tests yet.";
    return;
  }

  const rows = [...(brainLabProbeTestResult.rows || [])]
    .sort((left, right) => Number(right.maxAbsoluteOutputDelta || 0) - Number(left.maxAbsoluteOutputDelta || 0));
  const fingerprints = (brainLabProbeTestResult.fingerprints || [])
    .map(renderBrainLabBehaviorFingerprint)
    .join("");
  const profile = renderBrainLabBehaviorProfile(brainLabProbeTestResult.profile);
  brainLabProbeTests.innerHTML = `
    <div class="brain-lab-probe-tests-summary">
      <strong>${formatNumber(brainLabProbeTestResult.evaluatedFixtureCount)} setups</strong>
      <span>${escapeHtml(brainLabProbeTestResult.brainArchitectureKind || "unknown brain")}</span>
      <span>${escapeHtml(brainLabProbeTestResult.snapshotPath || "")}</span>
      ${brainLabProbeTestResult.skippedFixtureCount ? `<span>${formatNumber(brainLabProbeTestResult.skippedFixtureCount)} skipped</span>` : ""}
    </div>
    ${profile}
    <div class="brain-lab-behavior-fingerprints">
      ${fingerprints || `<span class="brain-lab-muted">No strong fingerprint labels yet.</span>`}
    </div>
    <table class="brain-lab-probe-tests-table">
      <thead>
        <tr>
          <th>Setup</th>
          <th>Labels</th>
          <th>Input Edits</th>
          <th>Changed</th>
          <th>Gate Flips</th>
          <th>Max Delta</th>
          <th>Top Outputs</th>
        </tr>
      </thead>
      <tbody>
        ${rows.map(renderBrainLabProbeTestRow).join("")}
      </tbody>
    </table>
  `;
}

function renderBrainLabBehaviorProfile(profile) {
  if (!profile) {
    return "";
  }

  const sections = (profile.sections || [])
    .map(renderBrainLabBehaviorProfileSection)
    .join("");
  return `
    <div class="brain-lab-behavior-profile">
      <div class="brain-lab-behavior-profile-heading">
        <strong>Behavior Profile</strong>
        <span>${escapeHtml(profile.summary || "No strong behavior profile yet.")}</span>
      </div>
      <div class="brain-lab-behavior-profile-grid">
        ${sections}
      </div>
    </div>
  `;
}

function renderBrainLabBehaviorProfileSection(section) {
  const traits = (section.traits || [])
    .slice(0, 6)
    .map((trait) => `<span>${escapeHtml(trait)}</span>`)
    .join("");
  const evidence = (section.evidence || []).length > 0
    ? `Evidence: ${section.evidence.join(", ")}`
    : "No direct evidence rows.";
  return `
    <div class="brain-lab-behavior-profile-section" title="${escapeHtml(evidence)}">
      <strong>${escapeHtml(section.name || section.key || "Profile")}</strong>
      <p>${escapeHtml(section.summary || "No clear signal.")}</p>
      <div class="brain-lab-behavior-profile-traits">
        ${traits || `<span class="brain-lab-muted">no traits</span>`}
      </div>
    </div>
  `;
}

function renderBrainLabProbeTestRow(row) {
  const topOutputs = (row.topOutputs || [])
    .map((output) => {
      const deltaClass = output.delta > 0 ? "is-positive" : output.delta < 0 ? "is-negative" : "";
      const gate = output.baselineActive === null || output.baselineActive === undefined
        ? ""
        : ` ${output.baselineActive ? "on" : "off"} -> ${output.modifiedActive ? "on" : "off"}`;
      return `<div><strong>${escapeHtml(output.name)}</strong> <span class="${deltaClass}">${formatBrainLabDelta(output.delta)}</span>${escapeHtml(gate)}</div>`;
    })
    .join("");
  const labels = (row.labels || [])
    .slice(0, 8)
    .map(renderBrainLabBehaviorLabel)
    .join("");
  const tags = (row.tags || []).length > 0
    ? `<code>${escapeHtml((row.tags || []).join(", "))}</code>`
    : `<code>${escapeHtml(row.path || "")}</code>`;
  return `
    <tr class="${Number(row.changedOutputCount || 0) > 0 ? "is-changed" : ""}">
      <td><strong>${escapeHtml(row.name)}</strong>${tags}</td>
      <td><div class="brain-lab-behavior-labels">${labels || `<span class="brain-lab-muted">no strong label</span>`}</div></td>
      <td>${formatNumber(row.overrideCount)}</td>
      <td>${formatNumber(row.changedOutputCount)}</td>
      <td>${formatNumber(row.gateFlipCount)}</td>
      <td>${formatBrainLabNumber(row.maxAbsoluteOutputDelta)}</td>
      <td>${topOutputs}</td>
    </tr>
  `;
}

function renderBrainLabBehaviorLabel(label) {
  return `
    <span class="brain-lab-behavior-label" title="${escapeHtml(label.category || "")}: ${escapeHtml(formatBrainLabNumber(label.strength || 0))}">
      ${escapeHtml(label.name || label.key || "label")}
    </span>
  `;
}

function renderBrainLabBehaviorFingerprint(fingerprint) {
  const evidence = (fingerprint.evidence || []).length > 0
    ? ` | ${fingerprint.evidence.join(", ")}`
    : "";
  return `
    <span class="brain-lab-behavior-fingerprint" title="${escapeHtml((fingerprint.description || "") + evidence)}">
      <strong>${escapeHtml(fingerprint.name || fingerprint.key || "fingerprint")}</strong>
      <span>${formatNumber(fingerprint.score || 0)}</span>
    </span>
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

  clearBrainLabPopulation();
  syncBrainLabInputControls(key, clamped);
  scheduleBrainLabEvaluate();
}

function resetBrainLabInput(key) {
  const input = brainLabEvaluation?.inputs?.find((candidate) => candidate.key === key);
  if (!input) {
    return;
  }

  delete brainLabOverrides[key];
  clearBrainLabPopulation();
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
  clearBrainLabPopulation();
  evaluateBrainLab();
}

function resetBrainLabOverrides() {
  brainLabOverrides = {};
  brainLabWorldProbeOverrideKeys = new Set();
  resetBrainLabWorldProbeEdits();
  resetBrainLabWorldProbeToggles();
  resetBrainLabWorldProbeEnvironment();
  renderBrainLabWorldProbe();
  clearBrainLabPopulation();
  evaluateBrainLab();
}

function applyBrainLabPreset() {
  if (brainLabEvaluation?.supportsRawInputOverrides === false) {
    return;
  }

  const inputs = brainLabEvaluation?.inputs || [];
  if (inputs.length === 0) {
    return;
  }

  brainLabOverrides = {};
  brainLabWorldProbeOverrideKeys = new Set();
  resetBrainLabWorldProbeEdits();
  resetBrainLabWorldProbeToggles();
  resetBrainLabWorldProbeEnvironment();
  const preset = brainLabPresetSelect?.value || "muteSound";
  if (preset === "muteSound") {
    setBrainLabGroupNeutral("Sound", inputs);
  } else if (preset === "noFood") {
    setBrainLabFoodNeutral(inputs);
  } else if (preset === "onlyPlants") {
    setBrainLabOnlyPlants(inputs);
  } else if (preset === "onlyMeatEggs") {
    setBrainLabOnlyMeatEggs(inputs);
  } else if (preset === "noContact") {
    setBrainLabGroupNeutral("Contact", inputs);
  } else if (preset === "hungry") {
    setBrainLabOverride("internal.hunger", 1, inputs);
    setBrainLabOverride("internal.energy_ratio", 0.2, inputs);
    setBrainLabOverride("internal.energy_surplus", 0, inputs);
    setBrainLabOverride("internal.energy_fullness", 0, inputs);
    setBrainLabOverride("internal.gut_fullness", 0, inputs);
    setBrainLabOverride("internal.fat_ratio", 0, inputs);
    setBrainLabOverride("internal.mass_burden", 0, inputs);
  } else if (preset === "full") {
    setBrainLabOverride("internal.hunger", 0, inputs);
    setBrainLabOverride("internal.energy_ratio", 1, inputs);
    setBrainLabOverride("internal.energy_surplus", 1, inputs);
    setBrainLabOverride("internal.energy_fullness", 1, inputs);
    setBrainLabOverride("internal.gut_fullness", 1, inputs);
    setBrainLabOverride("internal.fat_ratio", 1, inputs);
    setBrainLabOverride("internal.mass_burden", 1, inputs);
  } else if (preset === "readyToReproduce") {
    setBrainLabOverride("internal.reproduction_readiness", 1, inputs);
    setBrainLabOverride("internal.egg_reserve_ratio", 1, inputs);
    setBrainLabOverride("internal.energy_surplus", 1, inputs);
    setBrainLabOverride("internal.health_ratio", 1, inputs);
  }

  clearBrainLabPopulation();
  evaluateBrainLab();
}

function setBrainLabGroupNeutral(group, inputs) {
  for (const input of inputs.filter((candidate) => candidate.group === group)) {
    setBrainLabOverride(input.key, input.neutralValue, inputs);
  }
}

function setBrainLabFoodNeutral(inputs) {
  for (const input of inputs.filter(isBrainLabFoodInput)) {
    setBrainLabOverride(input.key, input.neutralValue, inputs);
  }
}

function setBrainLabOnlyPlants(inputs) {
  const plantDensity = findBrainLabInput("vision.plant_density", inputs)?.baselineValue ?? 0;
  const plantContact = findBrainLabInput("contact.plant_food", inputs)?.baselineValue ?? 0;
  for (const input of inputs.filter(isBrainLabMeatOrEggInput)) {
    setBrainLabOverride(input.key, input.neutralValue, inputs);
  }
  setBrainLabOverride("vision.food_density", plantDensity, inputs);
  setBrainLabOverride("contact.food", plantContact, inputs);
}

function setBrainLabOnlyMeatEggs(inputs) {
  const meatDensity = findBrainLabInput("vision.meat_density", inputs)?.baselineValue ?? 0;
  const meatContact = Math.max(
    findBrainLabInput("contact.meat_food", inputs)?.baselineValue ?? 0,
    findBrainLabInput("contact.egg_food", inputs)?.baselineValue ?? 0);
  for (const input of inputs.filter(isBrainLabPlantInput)) {
    setBrainLabOverride(input.key, input.neutralValue, inputs);
  }
  setBrainLabOverride("vision.food_density", meatDensity, inputs);
  setBrainLabOverride("contact.food", meatContact, inputs);
}

function setBrainLabOverride(key, value, inputs) {
  const input = findBrainLabInput(key, inputs);
  if (!input) {
    return;
  }

  const clamped = Math.min(Math.max(Number(value), Number(input.minimumValue)), Number(input.maximumValue));
  if (Math.abs(clamped - Number(input.baselineValue)) <= 0.0005) {
    delete brainLabOverrides[key];
  } else {
    brainLabOverrides[key] = clamped;
  }
}

function findBrainLabInput(key, inputs = brainLabEvaluation?.inputs || []) {
  return inputs.find((input) => input.key === key) ?? null;
}

function isBrainLabFoodInput(input) {
  return input.key === "vision.food_density"
    || isBrainLabPlantInput(input)
    || isBrainLabMeatOrEggInput(input)
    || input.key.startsWith("contact.food")
    || input.key.startsWith("contact.plant_")
    || input.key.startsWith("contact.meat_")
    || input.key.startsWith("contact.egg_")
    || input.key.startsWith("scent.meat")
    || input.key.startsWith("scent.rotten_meat");
}

function isBrainLabPlantInput(input) {
  return input.key.startsWith("vision.plant")
    || input.key.includes(".plant_")
    || input.key.startsWith("contact.plant");
}

function isBrainLabMeatOrEggInput(input) {
  return input.key.startsWith("vision.meat")
    || input.key.includes(".meat_")
    || input.key.includes(".egg_")
    || input.key.startsWith("contact.meat")
    || input.key.startsWith("contact.egg")
    || input.key.startsWith("scent.meat")
    || input.key.startsWith("scent.rotten_meat");
}

function clearBrainLabPopulation() {
  brainLabPopulationEvaluation = null;
  brainLabProbeTestResult = null;
  brainLabProfileComparisonResult = null;
  brainLabProfileComparisonCohortKey = null;
  renderBrainLabPopulation();
  renderBrainLabProbeTests();
  renderBrainLabProfileComparison();
}

function updateBrainLabButtons() {
  const hasSnapshot = Boolean(brainLabSnapshot);
  const hasCreature = Boolean(brainLabCreatureSelect?.value);
  const hasEvaluation = Boolean(brainLabEvaluation);
  const supportsOverrides = brainLabEvaluation?.supportsRawInputOverrides !== false;
  const hasWorldProbeEdits = brainLabWorldProbeEdited
    || brainLabWorldProbeHiddenKeys.size > 0
    || brainLabWorldProbeMutedSoundKeys.size > 0
    || isBrainLabWorldProbeEnvironmentActive();
  const worldProbeControlsDisabled = !hasSnapshot || !hasCreature || !hasEvaluation || !brainLabWorldProbeScene;
  if (evaluateBrainLabButton) {
    evaluateBrainLabButton.disabled = !hasSnapshot || !hasCreature;
  }
  if (exportBrainLabSpeciesButton) {
    exportBrainLabSpeciesButton.disabled = !hasSnapshot || !hasCreature;
  }
  if (exportBrainLabBrainButton) {
    exportBrainLabBrainButton.disabled = !hasSnapshot || !hasCreature;
  }
  if (muteBrainLabSoundButton) {
    muteBrainLabSoundButton.disabled = !hasEvaluation || !supportsOverrides;
  }
  if (resetBrainLabOverridesButton) {
    resetBrainLabOverridesButton.disabled = !hasEvaluation || (Object.keys(brainLabOverrides).length === 0 && !hasWorldProbeEdits);
  }
  if (applyBrainLabPresetButton) {
    applyBrainLabPresetButton.disabled = !hasEvaluation || !supportsOverrides;
  }
  if (compareBrainLabPopulationButton) {
    compareBrainLabPopulationButton.disabled = !hasSnapshot || !hasEvaluation;
  }
  if (runBrainLabPresetMatrixButton) {
    runBrainLabPresetMatrixButton.disabled = !hasSnapshot;
  }
  if (compareBrainLabProfilesButton) {
    compareBrainLabProfilesButton.disabled = !hasSnapshot || brainLabProfileComparisonRunning;
  }
  if (brainLabProfileScopeSelect) {
    brainLabProfileScopeSelect.disabled = !hasSnapshot || brainLabProfileComparisonRunning;
  }
  if (runBrainLabProbeTestsButton) {
    runBrainLabProbeTestsButton.disabled = !hasSnapshot || !hasCreature;
  }
  if (brainLabWorldProbeFixtureSelect) {
    brainLabWorldProbeFixtureSelect.disabled = brainLabWorldProbeFixtures.length === 0;
  }
  const selectedFixture = selectedBrainLabWorldProbeFixture();
  if (applyBrainLabWorldProbeFixtureButton) {
    applyBrainLabWorldProbeFixtureButton.disabled = worldProbeControlsDisabled || !selectedFixture;
  }
  if (saveBrainLabWorldProbeFixtureButton) {
    saveBrainLabWorldProbeFixtureButton.disabled = worldProbeControlsDisabled;
  }
  if (deleteBrainLabWorldProbeFixtureButton) {
    deleteBrainLabWorldProbeFixtureButton.disabled = !selectedFixture?.canDelete;
  }
  for (const control of [
    brainLabWorldProbeEnvironmentSelect,
    brainLabWorldProbeBiomeSelect,
    brainLabWorldProbeBoundarySelect,
    brainLabWorldProbeFertilityInput,
    brainLabWorldProbeObstacleSelect
  ]) {
    if (control) {
      control.disabled = worldProbeControlsDisabled;
    }
  }
  if (brainLabWorldProbeBoundaryOffsetInput) {
    brainLabWorldProbeBoundaryOffsetInput.disabled = worldProbeControlsDisabled || (brainLabWorldProbeBoundarySelect?.value || "none") === "none";
  }
  if (brainLabWorldProbe) {
    for (const control of brainLabWorldProbe.querySelectorAll("[data-brain-lab-world-toggle]")) {
      control.disabled = worldProbeControlsDisabled;
    }
  }
  for (const button of brainLabWorldProbeToolButtons || []) {
    button.disabled = worldProbeControlsDisabled;
  }
  updateBrainLabWorldProbeZoomControls();
  updateBrainLabWorldProbeActionButtons();
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
  const starterBrain = selectedSpeciesStarterBrainDetails(
    species,
    defaultBrain,
    defaultBrainStatus,
    defaultBrainMissing);

  speciesCatalogDetails.innerHTML = `
    <div class="species-summary-grid">
      <div><span>Name</span><strong>${escapeHtml(species.name)}</strong></div>
      <div><span>Path</span><strong>${escapeHtml(species.path)}</strong></div>
      <div><span>Starter brain</span><strong>${escapeHtml(starterBrain.name)}</strong></div>
      <div><span>Starter source</span><strong>${escapeHtml(starterBrain.source)}</strong></div>
      <div><span>Starter type</span><strong>${escapeHtml(starterBrain.architecture)}</strong></div>
      <div><span>Starter hidden nodes</span><strong>${escapeHtml(starterBrain.hiddenNodes)}</strong></div>
      <div><span>Starter status</span><strong class="${starterBrain.statusClass}">${escapeHtml(starterBrain.status)}</strong></div>
      <div><span>Profile default brain</span><strong>${escapeHtml(species.defaultBrainPath || "embedded profile brain")}</strong></div>
      <div><span>Profile embedded brain</span><strong>${escapeHtml(formatBrainArchitectureLabel(species.brainArchitectureKind))}, hidden ${formatNumber(species.brainHiddenNodeCount)}, ${formatNumber(species.brainWeightCount)} weights</strong></div>
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

function selectedSpeciesStarterBrainDetails(species, defaultBrain, defaultBrainStatus, defaultBrainMissing) {
  const choice = normalizeBrainChoiceValue(speciesSeedBrainSelect?.value || "profile");

  if (choice.startsWith("catalog:")) {
    const brainProfilePath = choice.slice("catalog:".length);
    const brain = findBrainCatalogEntryByPath(brainProfilePath);
    return {
      name: brain?.name || brainProfilePath,
      source: "Catalog brain profile",
      architecture: brain?.brainArchitectureKind ? formatBrainArchitectureLabel(brain.brainArchitectureKind) : "Unknown",
      hiddenNodes: brain ? formatBrainProfileTopology(brain) : "unknown",
      status: brain ? brainCompatibilityStatus(brain) : "Brain profile path is not present in the catalog.",
      statusClass: brain?.isCompatible === false || !brain ? "map-artifact-warning" : "map-artifact-ok"
    };
  }

  if (choice.startsWith("generated:")) {
    const kind = choice.slice("generated:".length);
    const scenarioArchitecture = scenarioBrainArchitectureKind();
    return {
      name: `${formatEnumLabel(kind)} generated starter`,
      source: "Legacy generated starter",
      architecture: formatBrainArchitectureLabel(scenarioArchitecture),
      hiddenNodes: brainArchitectureIsRtNeat(scenarioArchitecture)
        ? "graph topology"
        : `scenario hidden ${formatNumber(scenarioEditor?.scenario?.brainHiddenNodeCount ?? 0)}`,
      status: "Legacy generated starter. Prefer selecting an exact catalog brain profile.",
      statusClass: "map-artifact-warning"
    };
  }

  if (defaultBrain) {
    return {
      name: defaultBrain.name,
      source: "Profile default brain",
      architecture: formatBrainArchitectureLabel(defaultBrain.brainArchitectureKind),
      hiddenNodes: formatBrainProfileTopology(defaultBrain),
      status: defaultBrainStatus,
      statusClass: defaultBrain.isCompatible === false || defaultBrainMissing ? "map-artifact-warning" : "map-artifact-ok"
    };
  }

  return {
    name: "Embedded profile brain",
    source: "Species profile",
    architecture: formatBrainArchitectureLabel(species.brainArchitectureKind),
    hiddenNodes: brainArchitectureIsRtNeat(species.brainArchitectureKind)
      ? `graph topology, hidden ${formatNumber(species.brainHiddenNodeCount)}, ${formatNumber(species.brainWeightCount)} weights`
      : `hidden ${formatNumber(species.brainHiddenNodeCount)}, ${formatNumber(species.brainWeightCount)} weights`,
    status: defaultBrainStatus,
    statusClass: defaultBrainMissing ? "map-artifact-warning" : "map-artifact-ok"
  };
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
          <th>Brain</th>
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
  const brainChoiceValue = speciesSeedBrainChoiceValue(seed);
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
          ${brainChoiceOptionsHtml(brainChoiceValue, seed?.brainProfilePath || null, profile)}
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
  return speciesCatalog.find((candidate) => samePath(candidate.path, path)) ?? null;
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
  const selected = normalizeBrainChoiceValue(value || "profile");
  if (selected.startsWith("catalog:")) {
    const brainProfilePath = selected.slice("catalog:".length);
    return {
      brainOverrideKind: null,
      brainProfilePath
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
  renderSpeciesBrainChoiceOptions();
  renderSpeciesCatalogDetails();
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
    option.title = recipe.description || recipe.path;
    recipeOptions.append(option);
  }

  updateSelectedRecipeDescription();
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

function updateSelectedRecipeDescription() {
  if (!recipeDescription) {
    return;
  }

  const description = String(selectedRecipe()?.description ?? "").trim();
  recipeDescription.textContent = description;
  recipeDescription.hidden = description.length === 0;
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
    const description = String(recipe.description ?? "").trim();
    return `
      <div class="recipe-stack-item">
        <div>
          <strong>${escapeHtml(recipe.name)}</strong>
          <span>${fields.length} field${fields.length === 1 ? "" : "s"}${overrideCount > 0 ? `, overrides ${overrideCount}` : ""}</span>
          ${description ? `<p class="recipe-stack-description">${escapeHtml(description)}</p>` : ""}
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
  const wrapper = document.createElement(field.jsonName === "ecologicalEvents" ? "div" : "label");
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

  if (field.jsonName === "ecologicalEvents") {
    control = createEcologicalEventsControl(field, value);
  } else if (field.type === "boolean") {
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

  control.classList.add("scenario-control");
  control.dataset.jsonName = field.jsonName;
  control.dataset.type = field.type;
  return control;
}

function createEcologicalEventsControl(field, value) {
  const events = Array.isArray(value) ? value : [];
  const control = document.createElement("div");
  control.className = "ecological-event-editor";
  control.dataset.ecologicalEventEditor = "true";
  control.innerHTML = `
    <div class="ecological-event-toolbar">
      <span>${events.length === 0 ? "No events scheduled" : `${formatNumber(events.length)} scheduled`}</span>
      <button class="secondary" type="button" data-ecological-event-action="add">Add Event</button>
    </div>
    ${events.length === 0 ? "" : `
      <div class="ecological-event-rows">
        ${events.map((ecologicalEvent, index) => renderEcologicalEventRow(ecologicalEvent, index)).join("")}
      </div>
    `}
  `;
  return control;
}

function renderEcologicalEventRow(ecologicalEvent, index) {
  const event = normalizeEcologicalEvent(ecologicalEvent);
  return `
    <div class="ecological-event-row" data-index="${index}">
      <div class="ecological-event-row-header">
        <strong>${escapeHtml(event.name || ecologicalEventKindLabel(event.kind))}</strong>
        <div class="ecological-event-row-actions">
          <button class="secondary" type="button" data-ecological-event-action="duplicate" data-index="${index}">Duplicate</button>
          <button class="danger" type="button" data-ecological-event-action="remove" data-index="${index}">Remove</button>
        </div>
      </div>
      <label>
        Name
        <input type="text" value="${escapeHtml(event.name)}" data-ecological-event-field="name" data-index="${index}">
      </label>
      <label>
        Kind
        <select data-ecological-event-field="kind" data-index="${index}">
          ${ecologicalEventKinds.map(([value, label]) =>
            `<option value="${escapeHtml(value)}"${value === event.kind ? " selected" : ""}>${escapeHtml(label)}</option>`
          ).join("")}
        </select>
      </label>
      <label>
        Start (seconds)
        <input type="number" min="0" step="1" value="${escapeHtml(formatScenarioControlNumber(event.startSeconds))}" data-ecological-event-field="startSeconds" data-index="${index}">
      </label>
      <label>
        Duration (seconds)
        <input type="number" min="0.001" step="1" value="${escapeHtml(formatScenarioControlNumber(event.durationSeconds))}" data-ecological-event-field="durationSeconds" data-index="${index}">
      </label>
      <label>
        Strength
        <input type="number" min="0" step="0.01" value="${escapeHtml(formatScenarioControlNumber(event.strength))}" data-ecological-event-field="strength" data-index="${index}">
      </label>
      <label>
        X
        <input type="number" min="0" max="1" step="0.01" value="${escapeHtml(formatScenarioControlNumber(event.regionX))}" data-ecological-event-field="regionX" data-index="${index}">
      </label>
      <label>
        Y
        <input type="number" min="0" max="1" step="0.01" value="${escapeHtml(formatScenarioControlNumber(event.regionY))}" data-ecological-event-field="regionY" data-index="${index}">
      </label>
      <label>
        Width
        <input type="number" min="0.0001" max="1" step="0.01" value="${escapeHtml(formatScenarioControlNumber(event.regionWidth))}" data-ecological-event-field="regionWidth" data-index="${index}">
      </label>
      <label>
        Height
        <input type="number" min="0.0001" max="1" step="0.01" value="${escapeHtml(formatScenarioControlNumber(event.regionHeight))}" data-ecological-event-field="regionHeight" data-index="${index}">
      </label>
      <div class="ecological-event-summary">${escapeHtml(formatEcologicalEventSummary(event))}</div>
    </div>
  `;
}

function defaultEcologicalEvent(kind = "regionalFertilityPulse") {
  return {
    name: ecologicalEventKindLabel(kind),
    kind,
    startSeconds: 0,
    durationSeconds: 300,
    regionX: 0,
    regionY: 0,
    regionWidth: 1,
    regionHeight: 1,
    strength: defaultEcologicalEventStrength(kind)
  };
}

function normalizeEcologicalEvent(event) {
  const kind = ecologicalEventKinds.some(([value]) => value === event?.kind)
    ? event.kind
    : "regionalFertilityPulse";
  return {
    name: String(event?.name ?? "").trim(),
    kind,
    startSeconds: finiteNumberOrDefault(event?.startSeconds, 0),
    durationSeconds: finiteNumberOrDefault(event?.durationSeconds, 300),
    regionX: finiteNumberOrDefault(event?.regionX, 0),
    regionY: finiteNumberOrDefault(event?.regionY, 0),
    regionWidth: finiteNumberOrDefault(event?.regionWidth, 1),
    regionHeight: finiteNumberOrDefault(event?.regionHeight, 1),
    strength: finiteNumberOrDefault(event?.strength, defaultEcologicalEventStrength(kind))
  };
}

function finiteNumberOrDefault(value, fallback) {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function formatScenarioControlNumber(value) {
  const number = Number(value);
  return Number.isFinite(number) ? String(number) : "";
}

function ecologicalEventKindLabel(kind) {
  return ecologicalEventKinds.find(([value]) => value === kind)?.[1] || "Ecological event";
}

function defaultEcologicalEventStrength(kind) {
  if (kind === "regionalFertilityPulse") {
    return 2;
  }

  if (kind === "regionalFertilityCrash") {
    return 0.35;
  }

  return 0.2;
}

function formatEcologicalEventSummary(event) {
  const start = Number(event.startSeconds);
  const end = start + Number(event.durationSeconds);
  const strength = event.kind === "regionalFertilityPulse" || event.kind === "regionalFertilityCrash"
    ? `${formatDecimal(event.strength)}x fertility`
    : `${event.kind === "coldSnap" ? "-" : "+"}${formatDecimal(event.strength * 100)} temperature index`;
  return `${ecologicalEventKindLabel(event.kind)} from ${formatDecimal(start)}s to ${formatDecimal(end)}s, ${strength}, region ${formatPercent(event.regionX)}-${formatPercent(event.regionX + event.regionWidth)} x ${formatPercent(event.regionY)}-${formatPercent(event.regionY + event.regionHeight)}`;
}

function updateEcologicalEvents(action, index) {
  if (!scenarioEditor) {
    return;
  }

  try {
    storeVisibleScenarioValues();
  } catch (error) {
    scenarioOptionsStatus.textContent = error.message;
    return;
  }

  const events = Array.isArray(scenarioEditor.scenario.ecologicalEvents)
    ? cloneJson(scenarioEditor.scenario.ecologicalEvents).map(normalizeEcologicalEvent)
    : [];
  if (action === "add") {
    const last = events.at(-1);
    const next = defaultEcologicalEvent();
    if (last) {
      next.startSeconds = Number(last.startSeconds || 0) + Number(last.durationSeconds || 0);
    }

    events.push(next);
    formMessage.textContent = "Added ecological event.";
  } else if (action === "duplicate" && index >= 0 && index < events.length) {
    const copy = cloneJson(events[index]);
    copy.name = `${copy.name || ecologicalEventKindLabel(copy.kind)} copy`;
    copy.startSeconds = Number(copy.startSeconds || 0) + Number(copy.durationSeconds || 0);
    events.splice(index + 1, 0, copy);
    formMessage.textContent = "Duplicated ecological event.";
  } else if (action === "remove" && index >= 0 && index < events.length) {
    const [removed] = events.splice(index, 1);
    formMessage.textContent = `Removed ${removed.name || ecologicalEventKindLabel(removed.kind)}.`;
  }

  scenarioEditor.scenario.ecologicalEvents = events;
  renderScenarioEditor();
  updateScenarioManagementButtons();
}

function resetEcologicalEventStrengthOnKindChange(target) {
  const kindControl = target?.closest?.("[data-ecological-event-field='kind']");
  if (!kindControl) {
    return;
  }

  const row = kindControl.closest(".ecological-event-row");
  const strengthControl = row?.querySelector("[data-ecological-event-field='strength']");
  if (strengthControl) {
    strengthControl.value = String(defaultEcologicalEventStrength(kindControl.value));
  }
}

function readEcologicalEventsControlValue(control) {
  const rows = [...control.querySelectorAll(".ecological-event-row")];
  return rows.map((row, rowIndex) => {
    const event = {};
    for (const fieldControl of row.querySelectorAll("[data-ecological-event-field]")) {
      const field = fieldControl.dataset.ecologicalEventField;
      if (field === "name" || field === "kind") {
        event[field] = String(fieldControl.value ?? "").trim();
      } else {
        const raw = String(fieldControl.value ?? "").trim();
        if (raw === "") {
          throw new Error(`Ecological event ${rowIndex + 1} ${field} needs a numeric value.`);
        }

        const parsed = Number(raw);
        if (!Number.isFinite(parsed)) {
          throw new Error(`Ecological event ${rowIndex + 1} ${field} needs a numeric value.`);
        }

        event[field] = parsed;
      }
    }

    return validateEcologicalEventForEditor(event, rowIndex);
  });
}

function validateEcologicalEventForEditor(event, rowIndex) {
  const indexLabel = `Ecological event ${rowIndex + 1}`;
  if (!ecologicalEventKinds.some(([value]) => value === event.kind)) {
    throw new Error(`${indexLabel} needs an event kind.`);
  }

  requireNumberRange(event.startSeconds, 0, Number.POSITIVE_INFINITY, `${indexLabel} start`);
  requireNumberRange(event.durationSeconds, 0.001, Number.POSITIVE_INFINITY, `${indexLabel} duration`);
  requireNumberRange(event.regionX, 0, 1, `${indexLabel} X`);
  requireNumberRange(event.regionY, 0, 1, `${indexLabel} Y`);
  requireNumberRange(event.regionWidth, 0.0001, 1, `${indexLabel} width`);
  requireNumberRange(event.regionHeight, 0.0001, 1, `${indexLabel} height`);
  if (event.regionX + event.regionWidth > 1.000001) {
    throw new Error(`${indexLabel} X plus width must not exceed 1.`);
  }

  if (event.regionY + event.regionHeight > 1.000001) {
    throw new Error(`${indexLabel} Y plus height must not exceed 1.`);
  }

  const fertilityEvent = event.kind === "regionalFertilityPulse" || event.kind === "regionalFertilityCrash";
  requireNumberRange(event.strength, 0, fertilityEvent && event.kind === "regionalFertilityPulse" ? 10 : 1, `${indexLabel} strength`);
  return {
    name: event.name,
    kind: event.kind,
    startSeconds: event.startSeconds,
    durationSeconds: event.durationSeconds,
    regionX: event.regionX,
    regionY: event.regionY,
    regionWidth: event.regionWidth,
    regionHeight: event.regionHeight,
    strength: event.strength
  };
}

function requireNumberRange(value, minimum, maximum, label) {
  if (!Number.isFinite(value) || value < minimum || value > maximum) {
    throw new Error(`${label} must be between ${formatDecimal(minimum)} and ${maximum === Number.POSITIVE_INFINITY ? "infinity" : formatDecimal(maximum)}.`);
  }
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
  if (field.jsonName === "ecologicalEvents") {
    return readEcologicalEventsControlValue(control);
  }

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

  const brainChoice = parseSpeciesBrainChoice(speciesSeedBrainSelect?.value || defaultSpeciesBrainChoiceValue(species));
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

  if (brainOverrideKind) {
    return `legacy generated ${formatEnumLabel(brainOverrideKind)} brain`;
  }

  return "the profile/default brain";
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
  setLauncherTab("launch");
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
  setLauncherTab("brain");
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

launcherTabs?.addEventListener("click", (event) => {
  const button = event.target.closest("[data-launcher-tab]");
  if (!button) {
    return;
  }

  setLauncherTab(button.dataset.launcherTab);
});

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

speciesCatalogSelect.addEventListener("change", () => {
  renderSpeciesBrainChoiceOptions(defaultSpeciesBrainChoiceValue());
  renderSpeciesCatalogDetails();
});
speciesSeedBrainSelect.addEventListener("change", () => {
  renderSpeciesCatalogDetails();
});
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
exportBrainLabSpeciesButton.addEventListener("click", () => saveBrainLabSpeciesProfile());
exportBrainLabBrainButton.addEventListener("click", () => saveBrainLabBrainProfile());
muteBrainLabSoundButton.addEventListener("click", muteBrainLabSound);
resetBrainLabOverridesButton.addEventListener("click", resetBrainLabOverrides);
applyBrainLabPresetButton.addEventListener("click", applyBrainLabPreset);
compareBrainLabPopulationButton.addEventListener("click", compareBrainLabPopulation);
runBrainLabPresetMatrixButton.addEventListener("click", runBrainLabPresetMatrix);
compareBrainLabProfilesButton.addEventListener("click", compareBrainLabProfiles);
runBrainLabProbeTestsButton.addEventListener("click", runBrainLabProbeTests);
brainLabProfileComparison.addEventListener("click", (event) => {
  const cohortButton = event.target.closest("[data-brain-lab-profile-cohort]");
  if (cohortButton) {
    const cohortKey = cohortButton.dataset.brainLabProfileCohort || "";
    selectBrainLabProfileCohort(cohortKey === "__all" ? "" : cohortKey);
    return;
  }

  const profileActionButton = event.target.closest("[data-brain-lab-profile-action]");
  if (profileActionButton) {
    const creatureId = Number(profileActionButton.dataset.brainLabProfileActionCreature || 0);
    const creature = brainLabProfileComparisonCreature(creatureId);
    if (!creature) {
      brainLabStatus.textContent = `Creature #${formatNumber(creatureId)} is not in the current profile comparison.`;
      return;
    }

    if (profileActionButton.dataset.brainLabProfileAction === "export-species") {
      void saveBrainLabSpeciesProfile(creature);
    } else if (profileActionButton.dataset.brainLabProfileAction === "export-brain") {
      void saveBrainLabBrainProfile(creature);
    }

    return;
  }

  const creatureButton = event.target.closest("[data-brain-lab-profile-creature]");
  if (creatureButton) {
    void loadBrainLabProfileCreature(Number(creatureButton.dataset.brainLabProfileCreature || 0));
  }
});
brainLabWorldProbeFixtureSelect.addEventListener("change", updateBrainLabButtons);
applyBrainLabWorldProbeFixtureButton.addEventListener("click", applyBrainLabWorldProbeFixture);
saveBrainLabWorldProbeFixtureButton.addEventListener("click", saveBrainLabWorldProbeFixture);
deleteBrainLabWorldProbeFixtureButton.addEventListener("click", deleteBrainLabWorldProbeFixture);
brainLabWorldProbeEnvironmentSelect.addEventListener("change", applyBrainLabWorldProbeEnvironmentProfile);
brainLabWorldProbeBiomeSelect.addEventListener("change", markBrainLabWorldProbeEnvironmentCustom);
brainLabWorldProbeBoundarySelect.addEventListener("change", markBrainLabWorldProbeBoundaryCustom);
brainLabWorldProbeBoundaryOffsetInput.addEventListener("input", markBrainLabWorldProbeEnvironmentCustom);
brainLabWorldProbeFertilityInput.addEventListener("input", markBrainLabWorldProbeEnvironmentCustom);
brainLabWorldProbeObstacleSelect.addEventListener("change", markBrainLabWorldProbeEnvironmentCustom);
brainLabSnapshotSelect.addEventListener("change", () => {
  brainLabSnapshotPath.value = brainLabSnapshotSelect.value;
  if (brainLabSnapshotSelect.value) {
    loadBrainLabSnapshot();
  }
});
brainLabCreatureSelect.addEventListener("change", () => {
  brainLabOverrides = {};
  brainLabWorldProbeOverrideKeys = new Set();
  resetBrainLabWorldProbeEdits();
  resetBrainLabWorldProbeZoom(false);
  resetBrainLabWorldProbeToggles();
  brainLabWorldProbeScene = null;
  brainLabWorldProbeBaseScene = null;
  clearBrainLabPopulation();
  loadBrainLabWorldProbe().then(evaluateBrainLab);
});
brainLabGroupFilter.addEventListener("change", () => {
  renderBrainLabInputs();
  renderBrainLabWorldProbeTrace();
});
brainLabInputs.addEventListener("input", (event) => {
  const control = event.target.closest("[data-brain-lab-input-key]");
  if (control) {
    brainLabSelectedInputKey = control.dataset.brainLabInputKey;
    renderBrainLabWorldProbe();
    updateBrainLabInputOverride(control);
  }
});
brainLabInputs.addEventListener("click", (event) => {
  const button = event.target.closest("[data-brain-lab-reset-input]");
  if (button) {
    resetBrainLabInput(button.dataset.brainLabResetInput);
  }

  const row = event.target.closest("[data-brain-lab-input-row]");
  if (row) {
    selectBrainLabInput(row.dataset.brainLabInputRow);
  }
});
brainLabWorldProbe.addEventListener("change", (event) => {
  const editControl = event.target.closest("[data-brain-lab-world-edit-field]");
  if (editControl) {
    updateBrainLabWorldProbeEditorField(editControl);
    return;
  }

  const control = event.target.closest("[data-brain-lab-world-toggle]");
  if (control) {
    applyBrainLabWorldProbeToggles();
  }
});
for (const button of brainLabWorldProbeToolButtons || []) {
  button.addEventListener("click", () => setBrainLabWorldProbeTool(button.dataset.brainLabWorldTool || "select"));
}
brainLabWorldProbeCanvas.addEventListener("click", selectBrainLabWorldProbeAtEvent);
brainLabWorldProbeCanvas.addEventListener("pointerdown", beginBrainLabWorldProbePan);
brainLabWorldProbeCanvas.addEventListener("pointermove", moveBrainLabWorldProbePointer);
brainLabWorldProbeCanvas.addEventListener("pointerup", endBrainLabWorldProbePan);
brainLabWorldProbeCanvas.addEventListener("pointercancel", endBrainLabWorldProbePan);
brainLabWorldProbeCanvas.addEventListener("pointerleave", () => {
  if (!brainLabWorldProbePan && !brainLabWorldProbeDrag && !brainLabWorldProbeBoundaryDrag) {
    brainLabWorldProbeCanvas.style.cursor = "default";
  }
});
brainLabWorldProbeCanvas.addEventListener("wheel", zoomBrainLabWorldProbeWithWheel, { passive: false });
brainLabWorldProbeZoomOutButton.addEventListener("click", () => changeBrainLabWorldProbeZoom(1 / 1.25));
brainLabWorldProbeZoomInButton.addEventListener("click", () => changeBrainLabWorldProbeZoom(1.25));
brainLabWorldProbeZoomResetButton.addEventListener("click", () => resetBrainLabWorldProbeZoom());
hideBrainLabWorldProbeSelectionButton.addEventListener("click", hideSelectedBrainLabWorldProbeItem);
muteBrainLabWorldProbeSelectionSoundButton.addEventListener("click", muteSelectedBrainLabWorldProbeSound);
clearBrainLabWorldProbeEditsButton.addEventListener("click", clearBrainLabWorldProbeEdits);

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

scenarioFields.addEventListener("click", (event) => {
  const button = event.target.closest("button[data-ecological-event-action]");
  if (!button) {
    return;
  }

  updateEcologicalEvents(button.dataset.ecologicalEventAction, Number(button.dataset.index));
});

scenarioFields.addEventListener("change", (event) => {
  try {
    resetEcologicalEventStrengthOnKindChange(event.target);
    storeVisibleScenarioValues();
    updateScenarioFieldChangeMarkers();
    renderMapArtifactDetails();
    renderSpeciesBrainChoiceOptions();
    renderSpeciesCatalogDetails();
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
    renderSpeciesBrainChoiceOptions();
    renderSpeciesCatalogDetails();
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
  setLauncherTab("launch");
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
  await loadBrainLabWorldProbeFixtures();
  await loadSpeciesCatalog();
  await loadScenarioRecipes();
  await loadScenarios();
  await loadRuns();
  refreshTimer = setInterval(loadRuns, 2000);
}

setLauncherTab(activeLauncherTab);

boot().catch((error) => {
  refreshStatus.textContent = "Error";
  formMessage.textContent = error.message;
});
