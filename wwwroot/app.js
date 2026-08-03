/**
 * TechnoVIS - Application Controller (Vanilla JS)
 * =================================================
 * Architecture modulaire pour la gestion et la planification des visites de maintenance ECS.
 *
 * Modules principaux :
 * 1. ApiEngine : Communication REST API (avec basculement automatique vers fallback.js).
 * 2. Router : Gestion de la navigation par onglets (Tableau de Bord, Planning, Équipements, Clients, Technicien).
 * 3. DashboardView : Affichage des indicateurs KPI et des interventions prioritaires.
 * 4. PlanningView : Rendu du calendrier mensuel d'interventions et de la liste du planning.
 * 5. EquipementsView : Jauges de scoring risque MTBF et santé des équipements.
 * 6. ClientsView : Suivi des marchés de maintenance et répertoire clients.
 * 7. TechnicienView : Interface terrain pour la saisie des rapports de visite.
 * 8. ModalAndToastEngine : Gestionnaires de fenêtres modales et notifications.
 */

document.addEventListener("DOMContentLoaded", () => {
  App.init();
});

const App = {
  state: {
    currentTab: "dashboard",
    isOffline: false,
    stats: null,
    visites: [],
    equipements: [],
    clients: [],
    marches: [],
    currentMonth: new Date(2026, 7, 1) // Août 2026
  },

  init() {
    console.log("TechnoVIS Initialisation...");
    this.setupEventListeners();
    this.loadAllData();
  },

  /* ------------------------------------------------------------------------
   * 1. API ENGINE (Communication avec le backend C# / REST API)
   * ------------------------------------------------------------------------ */
  async fetchApi(endpoint, options = {}) {
    try {
      const response = await fetch(endpoint, options);
      if (!response.ok) throw new Error(`HTTP ${response.status}`);
      const data = await response.json();
      this.setOnlineStatus(true);
      // Normalize: ASP.NET sometimes wraps array results in {value:[...], Count:N}
      if (data && !Array.isArray(data) && Array.isArray(data.value)) {
        return data.value;
      }
      return data;
    } catch (error) {
      console.warn(`Erreur API (${endpoint}), passage en mode fallback offline:`, error);
      this.setOnlineStatus(false);
      return null;
    }
  },

  setOnlineStatus(isOnline) {
    this.state.isOffline = !isOnline;
    const dot = document.getElementById("network-dot");
    const text = document.getElementById("network-text");
    if (!dot || !text) return;

    if (isOnline) {
      dot.className = "dot-online";
      text.textContent = "API REST Connectée";
    } else {
      dot.className = "dot-offline";
      text.textContent = "Mode Hors-Ligne (Fallback)";
    }
  },

  async loadAllData() {
    // 1. Dashboard Stats
    let statsData = await this.fetchApi("/api/dashboard/stats");
    if (!statsData && window.TechnoVisFallback) {
      statsData = window.TechnoVisFallback.stats;
    }
    this.state.stats = statsData;

    // 2. Visites
    let visitesData = await this.fetchApi("/api/visites");
    if (!visitesData && window.TechnoVisFallback) {
      visitesData = window.TechnoVisFallback.visites;
    }
    this.state.visites = visitesData || [];

    // 3. Équipements
    let equipementsData = await this.fetchApi("/api/equipements");
    if (!equipementsData && window.TechnoVisFallback) {
      equipementsData = window.TechnoVisFallback.equipements;
    }
    this.state.equipements = equipementsData || [];

    // 4. Clients
    let clientsData = await this.fetchApi("/api/clients");
    if (!clientsData && window.TechnoVisFallback) {
      clientsData = window.TechnoVisFallback.clients;
    }
    this.state.clients = clientsData || [];

    // 5. Marchés
    let marchesData = await this.fetchApi("/api/marches");
    if (!marchesData && window.TechnoVisFallback) {
      marchesData = window.TechnoVisFallback.marches;
    }
    this.state.marches = marchesData || [];

    // Population des dropdowns pour les formulaires
    this.populateEquipementDropdown();
    this.populateClientDropdown();
    this.populateSiteDropdown();

    // Rendu des composants de la vue courante
    this.renderCurrentTab();
  },

  /* ------------------------------------------------------------------------
   * 2. ROUTER & EVENT LISTENERS
   * ------------------------------------------------------------------------ */
  setupEventListeners() {
    // Navigation par onglets dans la sidebar
    document.querySelectorAll(".nav-item").forEach(item => {
      item.addEventListener("click", (e) => {
        const tab = item.getAttribute("data-tab");
        if (tab) this.switchTab(tab);
      });
    });

    // Bouton d'actualisation
    document.getElementById("btn-refresh-data")?.addEventListener("click", () => {
      this.loadAllData();
      this.showToast("Données actualisées depuis l'API.");
    });

    // Boutons d'ouverture des modales
    document.getElementById("btn-open-modal-visite")?.addEventListener("click", () => {
      this.resetTechnicienDropdown();
      this.openModal("modal-visite");
    });
    document.getElementById("btn-open-modal-marche")?.addEventListener("click", () => {
      this.openModal("modal-marche");
    });
    document.getElementById("btn-open-modal-equipement")?.addEventListener("click", () => {
      this.openModal("modal-equipement");
    });

    // Fermeture des modales
    document.getElementById("close-modal-visite")?.addEventListener("click", () => this.closeModal("modal-visite"));
    document.getElementById("btn-cancel-visite")?.addEventListener("click", () => this.closeModal("modal-visite"));
    document.getElementById("close-modal-rapport")?.addEventListener("click", () => this.closeModal("modal-rapport"));
    document.getElementById("btn-cancel-rapport")?.addEventListener("click", () => this.closeModal("modal-rapport"));
    document.getElementById("close-modal-marche")?.addEventListener("click", () => this.closeModal("modal-marche"));
    document.getElementById("btn-cancel-marche")?.addEventListener("click", () => this.closeModal("modal-marche"));
    document.getElementById("close-modal-equipement")?.addEventListener("click", () => this.closeModal("modal-equipement"));
    document.getElementById("btn-cancel-equipement")?.addEventListener("click", () => this.closeModal("modal-equipement"));

    // Formulaires
    document.getElementById("form-new-visite")?.addEventListener("submit", (e) => this.handleCreateVisite(e));
    document.getElementById("form-rapport-technique")?.addEventListener("submit", (e) => this.handleUpdateRapport(e));
    document.getElementById("form-new-marche")?.addEventListener("submit", (e) => this.handleCreateMarche(e));
    document.getElementById("form-new-equipement")?.addEventListener("submit", (e) => this.handleCreateEquipement(e));

    // Contrôles du calendrier
    document.getElementById("cal-prev")?.addEventListener("click", () => {
      this.state.currentMonth.setMonth(this.state.currentMonth.getMonth() - 1);
      this.renderCalendar();
    });
    document.getElementById("cal-next")?.addEventListener("click", () => {
      this.state.currentMonth.setMonth(this.state.currentMonth.getMonth() + 1);
      this.renderCalendar();
    });
    document.getElementById("cal-today")?.addEventListener("click", () => {
      this.state.currentMonth = new Date(2026, 7, 1);
      this.renderCalendar();
    });

    // Filtre statut planning
    document.getElementById("filter-statut-visite")?.addEventListener("change", (e) => {
      this.renderPlanningTable(e.target.value);
    });

    // Technicien suggestion: triggered when equipement changes in the new-visite modal
    document.getElementById("form-visite-equipement")?.addEventListener("change", (e) => {
      const equipementId = parseInt(e.target.value);
      if (equipementId) {
        this.loadTechniciensSuggeres(equipementId);
      } else {
        this.resetTechnicienDropdown();
      }
    });
  },

  switchTab(tabKey) {
    this.state.currentTab = tabKey;
    document.querySelectorAll(".nav-item").forEach(el => el.classList.remove("active"));
    document.querySelectorAll(".tab-view").forEach(el => el.classList.remove("active"));

    const activeNav = document.querySelector(`.nav-item[data-tab="${tabKey}"]`);
    const activeView = document.getElementById(`tab-${tabKey}`);
    if (activeNav) activeNav.classList.add("active");
    if (activeView) activeView.classList.add("active");

    const titles = {
      dashboard: "Tableau de Bord Maintenance",
      planning: "Planification & Calendrier d'Interventions",
      equipements: "Parc d'Équipements & Scoring MTBF",
      clients: "Marchés & Répertoire Clients",
      technicien: "Mode Technicien Terrain"
    };

    const headerTitle = document.getElementById("header-page-title");
    if (headerTitle) headerTitle.textContent = titles[tabKey] || "TechnoVIS";

    this.renderCurrentTab();
  },

  renderCurrentTab() {
    switch (this.state.currentTab) {
      case "dashboard":
        this.renderDashboard();
        break;
      case "planning":
        this.renderCalendar();
        this.renderPlanningTable();
        break;
      case "equipements":
        this.renderEquipements();
        break;
      case "clients":
        this.renderClients();
        break;
      case "technicien":
        this.renderTechnicien();
        break;
    }
  },

  /* ------------------------------------------------------------------------
   * 3. DASHBOARD VIEW
   * ------------------------------------------------------------------------ */
  renderDashboard() {
    const stats = this.state.stats;
    if (!stats) return;

    document.getElementById("kpi-total-visites").textContent = stats.totalVisites ?? 0;
    document.getElementById("kpi-visites-planifiees").textContent = stats.visitesPlanifiees ?? 0;
    document.getElementById("kpi-visites-retard").textContent = stats.visitesEnRetard ?? 0;
    document.getElementById("kpi-equipements-critiques").textContent = stats.equipementsCritiques ?? 0;
    document.getElementById("kpi-taux-conformite").textContent = `${stats.tauxConformite ?? 100}%`;

    // ── Chart 1: Visites par Statut (bar chart) ──────────────────────────
    const ctx1 = document.getElementById("chart-visites-statut");
    if (ctx1 && typeof Chart !== "undefined") {
      if (ctx1._chartInstance) ctx1._chartInstance.destroy();

      const planifiees = stats.visitesPlanifiees ?? 0;
      const enRetard   = stats.visitesEnRetard ?? 0;
      const validees   = stats.visitesValidees ?? (stats.totalVisites - planifiees - enRetard);

      ctx1._chartInstance = new Chart(ctx1, {
        type: "bar",
        data: {
          labels: ["Planifiées", "En Retard", "Validées"],
          datasets: [{
            label: "Nombre de visites",
            data: [planifiees, enRetard, validees],
            backgroundColor: ["#1a9b8a", "#e05a5a", "#34c38f"],
            borderRadius: 6,
            borderSkipped: false
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: ctx => ` ${ctx.parsed.y} visite(s)` } }
          },
          scales: {
            x: { grid: { display: false }, ticks: { color: "#6e6e73", font: { family: "Inter, sans-serif" } } },
            y: { beginAtZero: true, grid: { color: "#e5e5ea" }, ticks: { color: "#6e6e73", stepSize: 1, precision: 0 } }
          }
        }
      });
    }

    // ── Chart 2: Répartition du Risque Équipements (horizontal bar) ───────
    const ctx2 = document.getElementById("chart-equipements-risque");
    if (ctx2 && typeof Chart !== "undefined" && this.state.equipements.length > 0) {
      if (ctx2._chartInstance) ctx2._chartInstance.destroy();

      const eqs = this.state.equipements;
      const faible   = eqs.filter(e => e.scoreRisque < 40).length;
      const moyen    = eqs.filter(e => e.scoreRisque >= 40 && e.scoreRisque < 70).length;
      const critique = eqs.filter(e => e.scoreRisque >= 70).length;

      ctx2._chartInstance = new Chart(ctx2, {
        type: "bar",
        data: {
          labels: ["Faible (< 40)", "Moyen (40-69)", "Critique (≥ 70)"],
          datasets: [{
            label: "Équipements",
            data: [faible, moyen, critique],
            backgroundColor: ["#34c38f", "#f5a623", "#e05a5a"],
            borderRadius: 6,
            borderSkipped: false
          }]
        },
        options: {
          indexAxis: "y",
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: { display: false },
            tooltip: { callbacks: { label: ctx => ` ${ctx.parsed.x} équipement(s)` } }
          },
          scales: {
            x: { beginAtZero: true, grid: { color: "#e5e5ea" }, ticks: { color: "#6e6e73", stepSize: 1, precision: 0 } },
            y: { grid: { display: false }, ticks: { color: "#6e6e73", font: { family: "Inter, sans-serif" } } }
          }
        }
      });
    }

    const tbody = document.getElementById("table-urgent-body");
    if (!tbody) return;
    tbody.innerHTML = "";

    const alertes = stats.alertesUrgent || [];
    if (alertes.length === 0) {
      tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: var(--text-muted); padding: 2rem;">Aucune alerte critique enregistrée.</td></tr>`;
      return;
    }

    alertes.forEach(a => {
      const tr = document.createElement("tr");
      const dateFormatted = new Date(a.datePrevue).toLocaleDateString("fr-FR");
      tr.innerHTML = `
        <td><strong>${a.reference}</strong></td>
        <td>${a.equipement}</td>
        <td>${a.site}</td>
        <td>${dateFormatted}</td>
        <td><span class="badge ${a.scorePriorite >= 80 ? 'badge-retard' : 'badge-planifiee'}">Prio ${a.scorePriorite}</span></td>
        <td><span class="badge ${this.getBadgeClass(a.statut)}">${a.statut}</span></td>
        <td>
          <button class="btn btn-secondary btn-sm" onclick="App.openRapportModal(${a.id})">Traiter</button>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  /* ------------------------------------------------------------------------
   * 4. PLANNING & CALENDAR VIEW
   * ------------------------------------------------------------------------ */
  renderCalendar() {
    const grid = document.getElementById("calendar-grid");
    const label = document.getElementById("calendar-month-label");
    if (!grid || !label) return;

    grid.innerHTML = "";
    const date = this.state.currentMonth;
    const year = date.getFullYear();
    const month = date.getMonth();

    const monthNames = ["Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"];
    label.textContent = `${monthNames[month]} ${year}`;

    // En-têtes des jours de la semaine
    const days = ["Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim"];
    days.forEach(d => {
      const h = document.createElement("div");
      h.className = "calendar-day-header";
      h.textContent = d;
      grid.appendChild(h);
    });

    // Calcul du premier jour du mois
    const firstDay = new Date(year, month, 1).getDay();
    const startingDay = firstDay === 0 ? 6 : firstDay - 1; // 0 = Lundi
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    // Cellules vides de début
    for (let i = 0; i < startingDay; i++) {
      const emptyCell = document.createElement("div");
      emptyCell.className = "calendar-day-cell";
      emptyCell.style.opacity = "0.3";
      grid.appendChild(emptyCell);
    }

    // Cellules des jours du mois
    for (let day = 1; day <= daysInMonth; day++) {
      const cell = document.createElement("div");
      cell.className = "calendar-day-cell";

      const num = document.createElement("div");
      num.className = "calendar-day-number";
      num.textContent = day;
      cell.appendChild(num);

      // Détection des visites ce jour-là
      const currentDateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
      const dayVisites = this.state.visites.filter(v => v.datePrevue.startsWith(currentDateStr));

      dayVisites.forEach(v => {
        const ev = document.createElement("div");
        ev.className = `calendar-event ${v.statut === 'En retard' ? 'event-retard' : ''}`;
        ev.textContent = `${v.reference} - ${v.equipementNom}`;
        ev.title = `${v.typeVisite} sur ${v.equipementNom} (${v.technicienAssigne})`;
        ev.onclick = () => this.openRapportModal(v.id);
        cell.appendChild(ev);
      });

      grid.appendChild(cell);
    }
  },

  renderPlanningTable(filterStatut = "") {
    const tbody = document.getElementById("table-planning-body");
    if (!tbody) return;
    tbody.innerHTML = "";

    let list = this.state.visites;
    if (filterStatut) {
      list = list.filter(v => v.statut === filterStatut);
    }

    if (list.length === 0) {
      tbody.innerHTML = `<tr><td colspan="9" style="text-align: center; color: var(--text-muted); padding: 2rem;">Aucune visite trouvée.</td></tr>`;
      return;
    }

    list.forEach(v => {
      const tr = document.createElement("tr");
      const dateFormatted = new Date(v.datePrevue).toLocaleDateString("fr-FR") + " " + new Date(v.datePrevue).toLocaleTimeString("fr-FR", { hour: '2-digit', minute: '2-digit' });
      tr.innerHTML = `
        <td><strong>${v.reference}</strong></td>
        <td>${v.typeVisite}</td>
        <td>${v.equipementNom}</td>
        <td><span style="color: var(--text-secondary);">${v.clientNom}</span><br><small>${v.siteNom}</small></td>
        <td>${v.technicienAssigne}</td>
        <td>${dateFormatted}</td>
        <td><strong>${v.scorePriorite}</strong></td>
        <td><span class="badge ${this.getBadgeClass(v.statut)}">${v.statut}</span></td>
        <td>
          <button class="btn btn-secondary btn-sm" onclick="App.openRapportModal(${v.id})">Rapport</button>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  /* ------------------------------------------------------------------------
   * 5. EQUIPEMENTS VIEW & SCORING GAUGES
   * ------------------------------------------------------------------------ */
  renderEquipements() {
    const tbody = document.getElementById("table-equipements-body");
    if (!tbody) return;
    tbody.innerHTML = "";

    this.state.equipements.forEach(e => {
      const tr = document.createElement("tr");
      const scoreClass = e.scoreRisque >= 70 ? "score-high" : (e.scoreRisque >= 40 ? "score-med" : "score-low");
      const dateVisite = new Date(e.derniereVisite).toLocaleDateString("fr-FR");

      tr.innerHTML = `
        <td><code>${e.serialNumber}</code></td>
        <td><strong>${e.nom}</strong></td>
        <td>${e.categorie}</td>
        <td>${e.clientNom} <br><small style="color: var(--text-muted);">${e.siteNom}</small></td>
        <td>${e.scoreSante}%</td>
        <td>
          <div class="score-gauge-container">
            <div class="score-bar-bg">
              <div class="score-bar-fill ${scoreClass}" style="width: ${e.scoreRisque}%;"></div>
            </div>
            <span class="score-number">${e.scoreRisque}</span>
          </div>
        </td>
        <td><span class="badge ${e.statut === 'Opérationnel' ? 'badge-validee' : 'badge-revision'}">${e.statut}</span></td>
        <td>${dateVisite}</td>
      `;
      tbody.appendChild(tr);
    });
  },

  /* ------------------------------------------------------------------------
   * 6. CLIENTS & MARCHÉS VIEW
   * ------------------------------------------------------------------------ */
  renderClients() {
    // 1. Table Marchés
    const tbodyMarches = document.getElementById("table-marches-body");
    if (tbodyMarches) {
      tbodyMarches.innerHTML = "";
      this.state.marches.forEach(m => {
        const tr = document.createElement("tr");
        // DTO projection flattens client name into clientNom; fallback covers nested .client.nomSociete
        const clientNom = m.clientNom || (m.client && m.client.nomSociete) || "N/A";
        tr.innerHTML = `
          <td><strong>${m.codeMarche}</strong></td>
          <td>${m.libelle}</td>
          <td>${clientNom}</td>
          <td>${new Date(m.dateDebut).toLocaleDateString("fr-FR")} → ${new Date(m.dateFin).toLocaleDateString("fr-FR")}</td>
          <td><strong>${m.slaHeures}h</strong></td>
          <td>${m.visitesRealisees} / ${m.visitesAnnuellesPrevues}</td>
          <td><span class="badge badge-validee">${m.statut}</span></td>
        `;
        tbodyMarches.appendChild(tr);
      });
    }

    // 2. Table Clients
    const tbodyClients = document.getElementById("table-clients-body");
    if (tbodyClients) {
      tbodyClients.innerHTML = "";
      this.state.clients.forEach(c => {
        const tr = document.createElement("tr");
        const sitesCount = c.sites ? c.sites.length : 1;
        tr.innerHTML = `
          <td><code>${c.codeClient}</code></td>
          <td><strong>${c.nomSociete}</strong></td>
          <td>${c.contactPrincipal}</td>
          <td>${c.email}</td>
          <td>${c.telephone}</td>
          <td><span class="badge badge-planifiee">${sitesCount} Site(s)</span></td>
        `;
        tbodyClients.appendChild(tr);
      });
    }
  },

  /* ------------------------------------------------------------------------
   * 7. TECHNICIEN FIELD VIEW
   * ------------------------------------------------------------------------ */
  renderTechnicien() {
    const tbody = document.getElementById("table-technicien-body");
    if (!tbody) return;
    tbody.innerHTML = "";

    // Filtrer les visites du technicien connecté
    const mesVisites = this.state.visites.filter(v => v.technicienAssigne.includes("Amine") || v.technicienAssigne.includes("Hassan"));

    mesVisites.forEach(v => {
      const tr = document.createElement("tr");
      const dateFormatted = new Date(v.datePrevue).toLocaleDateString("fr-FR");
      tr.innerHTML = `
        <td><strong>${v.reference}</strong></td>
        <td>${v.typeVisite}</td>
        <td>${v.equipementNom}</td>
        <td>${v.siteNom}</td>
        <td>${dateFormatted}</td>
        <td><span class="badge ${this.getBadgeClass(v.statut)}">${v.statut}</span></td>
        <td>
          <button class="btn btn-primary btn-sm" onclick="App.openRapportModal(${v.id})">Compléter Fiche</button>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  /* ------------------------------------------------------------------------
   * 8. MODALS & TOAST HANDLERS
   * ------------------------------------------------------------------------ */
  populateEquipementDropdown() {
    const select = document.getElementById("form-visite-equipement");
    if (!select) return;
    // Blank first option so user must consciously choose
    select.innerHTML = `<option value="">— Choisir un équipement —</option>`;
    this.state.equipements.forEach(e => {
      const opt = document.createElement("option");
      opt.value = e.id;
      opt.textContent = `${e.nom} (${e.serialNumber}) — ${e.siteNom}`;
      select.appendChild(opt);
    });
  },

  // Reset the technicien dropdown to its disabled placeholder state
  resetTechnicienDropdown() {
    const sel = document.getElementById("form-visite-technicien");
    if (!sel) return;
    sel.innerHTML = `<option value="">Sélectionnez un équipement d'abord</option>`;
    sel.disabled = true;
  },

  /**
   * Fetch the ranked technician suggestions for a given equipement.
   * Uses the first visite in state matching that equipement as context for the
   * scoring endpoint. Falls back to GET /api/techniciens if no visite exists yet.
   */
  async loadTechniciensSuggeres(equipementId) {
    const sel = document.getElementById("form-visite-technicien");
    if (!sel) return;

    // Show loading state
    sel.innerHTML = `<option value="">Chargement des suggestions…</option>`;
    sel.disabled = true;

    // Find a visite for this equipement to use as scoring context
    const matchingVisite = this.state.visites.find(v => v.equipementId === equipementId);

    let suggestions = null;
    if (matchingVisite) {
      suggestions = await this.fetchApi(`/api/visites/${matchingVisite.id}/techniciens-suggeres`);
    }

    // Fallback: plain list from /api/techniciens (no score)
    if (!suggestions) {
      const techniciens = await this.fetchApi("/api/techniciens");
      if (techniciens) {
        suggestions = techniciens.map(t => ({ technicien: t, score: 0 }));
      }
    }

    if (!suggestions || suggestions.length === 0) {
      sel.innerHTML = `<option value="">Aucun technicien disponible</option>`;
      sel.disabled = true;
      return;
    }

    // Build option list ranked best → worst
    sel.innerHTML = `<option value="">— Choisir un technicien —</option>`;
    suggestions.forEach(s => {
      const t = s.technicien;
      const opt = document.createElement("option");
      // Store full name as the value (matches TechnicienAssigne field in backend)
      opt.value = `${t.prenom} ${t.nom}`;
      const scoreLabel = s.score > 0 ? ` — ${s.score}%` : "";
      const disponLabel = t.disponible ? "" : " ⚠ Indisponible";
      opt.textContent = `${t.prenom} ${t.nom}${scoreLabel}${disponLabel}`;
      if (!t.disponible) opt.style.color = "var(--accent-orange, #f5a623)";
      sel.appendChild(opt);
    });
    sel.disabled = false;
  },

  populateClientDropdown() {
    const select = document.getElementById("form-marche-client");
    if (!select) return;
    select.innerHTML = "";
    this.state.clients.forEach(c => {
      const opt = document.createElement("option");
      opt.value = c.id;
      opt.textContent = `${c.nomSociete} (${c.codeClient})`;
      select.appendChild(opt);
    });
  },

  populateSiteDropdown() {
    const select = document.getElementById("form-equipement-site");
    if (!select) return;
    select.innerHTML = "";
    this.state.clients.forEach(c => {
      const sites = c.sites || [];
      sites.forEach(s => {
        const opt = document.createElement("option");
        opt.value = s.id;
        opt.textContent = `${s.nomSite} (${s.ville}) — ${c.nomSociete}`;
        select.appendChild(opt);
      });
    });
  },

  openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.add("active");
  },

  closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.remove("active");
  },

  openRapportModal(visiteId) {
    const visite = this.state.visites.find(v => v.id === visiteId);
    if (!visite) return;

    document.getElementById("form-rapport-id").value = visite.id;
    document.getElementById("form-rapport-statut").value = visite.statut === "En retard" ? "En retard" : "Validée";
    document.getElementById("form-rapport-texte").value = visite.rapportTechnique || "";
    document.getElementById("form-rapport-actions").value = visite.actionsCorrectives || "";

    this.openModal("modal-rapport");
  },

  async handleCreateVisite(e) {
    e.preventDefault();
    const equipementId = parseInt(document.getElementById("form-visite-equipement").value);
    const typeVisite = document.getElementById("form-visite-type").value;
    const technicien = document.getElementById("form-visite-technicien").value;
    const datePrevue = document.getElementById("form-visite-date").value;

    const payload = {
      equipementId,
      typeVisite,
      technicienAssigne: technicien,
      datePrevue: new Date(datePrevue).toISOString(),
      statut: "Planifiée"
    };

    let result = await this.fetchApi("/api/visites", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    if (!result) {
      // Fallback local addition if offline
      const eq = this.state.equipements.find(x => x.id === equipementId);
      result = {
        id: Date.now(),
        reference: `VIS-2026-${Math.floor(1000 + Math.random() * 9000)}`,
        typeVisite,
        equipementId,
        equipementNom: eq ? eq.nom : "Équipement",
        siteNom: eq ? eq.siteNom : "Site",
        clientNom: eq ? eq.clientNom : "Client",
        technicienAssigne: technicien,
        datePrevue,
        statut: "Planifiée",
        scorePriorite: 70.0
      };
      this.state.visites.unshift(result);
    }

    this.closeModal("modal-visite");
    this.showToast(`Nouvelle visite ${result.reference || ''} planifiée avec succès !`);
    this.loadAllData();
  },

  async handleUpdateRapport(e) {
    e.preventDefault();
    const id = parseInt(document.getElementById("form-rapport-id").value);
    const statut = document.getElementById("form-rapport-statut").value;
    const rapportTechnique = document.getElementById("form-rapport-texte").value;
    const actionsCorrectives = document.getElementById("form-rapport-actions").value;

    const payload = { statut, rapportTechnique, actionsCorrectives };

    let res = await this.fetchApi(`/api/visites/${id}/statut`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    // Update state locally
    const item = this.state.visites.find(v => v.id === id);
    if (item) {
      item.statut = statut;
      item.rapportTechnique = rapportTechnique;
      item.actionsCorrectives = actionsCorrectives;
    }

    this.closeModal("modal-rapport");
    this.showToast("Fiche de visite enregistrée et validée.");
    this.renderCurrentTab();
  },

  async handleCreateMarche(e) {
    e.preventDefault();
    const codeMarche = document.getElementById("form-marche-code").value;
    const libelle = document.getElementById("form-marche-libelle").value;
    const clientId = parseInt(document.getElementById("form-marche-client").value);
    const dateDebut = document.getElementById("form-marche-datedebut").value;
    const dateFin = document.getElementById("form-marche-datefin").value;
    const slaHeures = parseInt(document.getElementById("form-marche-sla").value);
    const visitesAnnuellesPrevues = parseInt(document.getElementById("form-marche-visites").value);
    const statut = document.getElementById("form-marche-statut").value;

    const payload = {
      codeMarche,
      libelle,
      clientId,
      dateDebut: new Date(dateDebut).toISOString(),
      dateFin: new Date(dateFin).toISOString(),
      slaHeures,
      visitesAnnuellesPrevues,
      visitesRealisees: 0,
      statut
    };

    let result = await this.fetchApi("/api/marches", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    if (!result) {
      const client = this.state.clients.find(c => c.id === clientId);
      result = {
        id: Date.now(),
        codeMarche: codeMarche || `MAR-2026-${Math.floor(100 + Math.random() * 900)}`,
        libelle,
        clientId,
        clientNom: client ? client.nomSociete : "Client",
        dateDebut,
        dateFin,
        slaHeures,
        visitesAnnuellesPrevues,
        visitesRealisees: 0,
        statut
      };
      this.state.marches.unshift(result);
    }

    this.closeModal("modal-marche");
    this.showToast(`Nouveau marché ${result.codeMarche || ''} créé avec succès !`);
    this.loadAllData();
  },

  async handleCreateEquipement(e) {
    e.preventDefault();
    const nom = document.getElementById("form-equipement-nom").value;
    const serialNumber = document.getElementById("form-equipement-serial").value;
    const categorie = document.getElementById("form-equipement-categorie").value;
    const siteId = parseInt(document.getElementById("form-equipement-site").value);
    const criticiticite = parseInt(document.getElementById("form-equipement-criticite").value);
    const scoreSante = parseInt(document.getElementById("form-equipement-sante").value);
    const dateInstallation = document.getElementById("form-equipement-date").value;
    const statut = document.getElementById("form-equipement-statut").value;

    const payload = {
      nom,
      serialNumber: serialNumber || `EQ-${categorie.substring(0, 3).toUpperCase()}-${Math.floor(100 + Math.random() * 900)}`,
      categorie,
      siteId,
      criticiticite,
      scoreSante,
      dateInstallation: new Date(dateInstallation).toISOString(),
      statut,
      derniereVisite: new Date().toISOString(),
      prochaineVisitePrevue: new Date().toISOString()
    };

    let result = await this.fetchApi("/api/equipements", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });

    if (!result) {
      result = {
        id: Date.now(),
        ...payload,
        scoreRisque: Math.max(10, 100 - scoreSante)
      };
      this.state.equipements.unshift(result);
    }

    this.closeModal("modal-equipement");
    this.showToast(`Équipement ${result.nom || ''} ajouté avec succès !`);
    this.loadAllData();
  },

  getBadgeClass(statut) {
    switch (statut) {
      case "Validée": return "badge-validee";
      case "En retard": return "badge-retard";
      case "En cours": return "badge-revision";
      default: return "badge-planifiee";
    }
  },

  showToast(message) {
    const container = document.getElementById("toast-container");
    if (!container) return;

    const toast = document.createElement("div");
    toast.className = "toast";
    toast.textContent = message;

    container.appendChild(toast);
    setTimeout(() => {
      toast.style.opacity = "0";
      setTimeout(() => toast.remove(), 300);
    }, 3500);
  }
};
