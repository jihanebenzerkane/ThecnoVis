/**
 * TechnoVIS - Application Controller (Vanilla JS)
 * =================================================
 * Architecture complète pour la gestion des équipements, techniciens,
 * moteur de scoring dynamique, planification intelligente, exports PDF/Excel,
 * volet latéral KPI (Side Drawer), réinitialisation des données et personnalisation (White-Label).
 */

document.addEventListener("DOMContentLoaded", () => {
  App.init();
});

const DEFAULT_COMPANY_SETTINGS = {
  companyName: "TechnoVIS",
  companySlogan: "Plateforme Maintenance Industrielle Multi-Sites",
  companyEmail: "contact@technovis.ma",
  companyPhone: "+212 5 22 00 00 00",
  companyAddress: "Casablanca, Maroc",
  primaryColor: "#0d9488",
  themeMode: "light",
  agences: ["Casablanca", "Rabat", "Tanger", "Safi", "Marrakech", "Agadir", "Fès"],
  defaultHours: 40,
  defaultSla: 24,
  defaultCurrency: "MAD",
  defaultVisiteDuration: 120
};

const App = {
  state: {
    user: null,
    currentTab: "dashboard",
    isOffline: false,
    settings: { ...DEFAULT_COMPANY_SETTINGS },
    stats: null,
    visites: [],
    equipements: [],
    techniciens: [],
    specialites: [],
    clients: [],
    marches: [],
    selectedTechnicienIdForPlanning: null,
    currentMonth: new Date(2026, 7, 1) // Août 2026
  },

  async init() {
    console.log("TechnoVIS Initialisation...");

    // 1. Restaurer l'état du panneau latéral
    if (localStorage.getItem("technovis_sidebar_collapsed") === "1") {
      document.body.classList.add("sidebar-collapsed");
    }

    // 2. Configurer tous les écouteurs d'événements
    this.setupEventListeners();
    this.setupAuthListeners();
    this.setupKpiDrawerListeners();

    // 3. Vérifier la session utilisateur existante
    const isAuthenticated = await this.checkAuth();

    if (isAuthenticated) {
      this.showApp();
      await this.loadCompanySettings();
      await this.loadAllData();
    } else {
      this.showAuthScreen();
    }

    // 4. Masquer le preloader
    const preloader = document.getElementById("preloader");
    if (preloader) {
      preloader.classList.add("preloader-hide");
      setTimeout(() => preloader.style.display = 'none', 600);
    }
  },

  /* ------------------------------------------------------------------------
   * 0a. AUTHENTIFICATION & GESTION DE SESSION
   * ------------------------------------------------------------------------ */
  async checkAuth() {
    try {
      const user = await this.fetchApi("/api/auth/me");
      if (user && user.id) {
        this.state.user = user;
        this.updateUserProfileUI();
        return true;
      }
      this.state.user = null;
      return false;
    } catch {
      this.state.user = null;
      return false;
    }
  },

  showAuthScreen() {
    const authContainer = document.getElementById("auth-container");
    const appEl = document.getElementById("app");
    if (authContainer) authContainer.style.display = "flex";
    if (appEl) appEl.style.display = "none";
    this.switchAuthView("login");
  },

  showApp() {
    const authContainer = document.getElementById("auth-container");
    const appEl = document.getElementById("app");
    if (authContainer) authContainer.style.display = "none";
    if (appEl) appEl.style.display = "flex";
  },

  switchAuthView(viewName) {
    const loginView = document.getElementById("auth-view-login");
    const forgotView = document.getElementById("auth-view-forgot");
    const resetView = document.getElementById("auth-view-reset");

    if (loginView) loginView.style.display = viewName === "login" ? "block" : "none";
    if (forgotView) forgotView.style.display = viewName === "forgot" ? "block" : "none";
    if (resetView) resetView.style.display = viewName === "reset" ? "block" : "none";

    const loginErr = document.getElementById("auth-login-error");
    if (loginErr) { loginErr.style.display = "none"; loginErr.textContent = ""; }
    const forgotAlert = document.getElementById("auth-forgot-alert");
    if (forgotAlert) { forgotAlert.style.display = "none"; forgotAlert.textContent = ""; }
    const resetAlert = document.getElementById("auth-reset-alert");
    if (resetAlert) { resetAlert.style.display = "none"; resetAlert.textContent = ""; }
  },

  updateUserProfileUI() {
    const u = this.state.user;
    if (!u) return;

    const initials = (u.nomComplet || u.email || "TV")
      .split(" ")
      .map(part => part[0])
      .slice(0, 2)
      .join("")
      .toUpperCase();

    // Sidebar widgets
    const avatarEl = document.getElementById("user-avatar-initials");
    const nameEl = document.getElementById("user-display-name");
    const roleEl = document.getElementById("user-display-role");

    if (avatarEl) avatarEl.textContent = initials;
    if (nameEl) nameEl.textContent = u.nomComplet || u.email;
    if (roleEl) roleEl.textContent = u.role === "Responsable" ? "Responsable Maintenance" : `Technicien (${u.matricule || 'Terrain'})`;

    // Modal Profile widgets
    const modalAvatar = document.getElementById("profile-modal-avatar");
    const modalName = document.getElementById("profile-modal-name");
    const modalEmail = document.getElementById("profile-modal-email");
    const modalRole = document.getElementById("profile-modal-role");
    const modalMatricule = document.getElementById("profile-modal-matricule");
    const modalBase = document.getElementById("profile-modal-base");
    const modalDate = document.getElementById("profile-modal-date");

    if (modalAvatar) modalAvatar.textContent = initials;
    if (modalName) modalName.textContent = u.nomComplet || u.email;
    if (modalEmail) modalEmail.textContent = u.email;
    if (modalRole) {
      modalRole.textContent = u.role;
      modalRole.className = `badge ${u.role === 'Responsable' ? 'badge-planifiee' : 'badge-validee'}`;
    }
    if (modalMatricule) modalMatricule.textContent = u.matricule || "N/A (Compte Direction)";
    if (modalBase) modalBase.textContent = u.base || "Toutes les bases (Siège)";
    if (modalDate) {
      modalDate.textContent = u.dateCreation
        ? new Date(u.dateCreation).toLocaleDateString("fr-FR", { day: "2-digit", month: "long", year: "numeric" })
        : "—";
    }
  },

  setupAuthListeners() {
    // 1. Soumission Login
    document.getElementById("form-auth-login")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const identifier = document.getElementById("login-identifier")?.value?.trim();
      const password = document.getElementById("login-password")?.value;
      const errorDiv = document.getElementById("auth-login-error");
      const submitBtn = document.getElementById("btn-submit-login");

      if (errorDiv) { errorDiv.style.display = "none"; errorDiv.textContent = ""; }
      if (submitBtn) { submitBtn.disabled = true; submitBtn.innerHTML = `<span>Connexion en cours…</span>`; }

      try {
        const res = await this.fetchApi("/api/auth/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email: identifier, password })
        });

        if (res && res.email) {
          this.state.user = res;
          this.updateUserProfileUI();
          this.showApp();
          this.showToast(`Bienvenue, ${res.nomComplet || res.email} !`);
          await this.loadCompanySettings();
          await this.loadAllData();
        }
      } catch (err) {
        if (errorDiv) {
          errorDiv.textContent = err.message || "Identifiant ou mot de passe incorrect.";
          errorDiv.style.display = "block";
        }
      } finally {
        if (submitBtn) {
          submitBtn.disabled = false;
          submitBtn.innerHTML = `<span>Se connecter</span><span class="auth-btn-arrow">→</span>`;
        }
      }
    });

    // 2. Boutons de navigation Auth
    document.getElementById("btn-goto-forgot")?.addEventListener("click", () => {
      this.switchAuthView("forgot");
    });
    document.getElementById("btn-back-to-login")?.addEventListener("click", () => {
      this.switchAuthView("login");
    });
    document.getElementById("btn-back-to-forgot")?.addEventListener("click", () => {
      this.switchAuthView("forgot");
    });

    // 3. Soumission Mot de passe oublié (Email)
    document.getElementById("form-auth-forgot")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const email = document.getElementById("forgot-email")?.value?.trim();
      const alertDiv = document.getElementById("auth-forgot-alert");
      const submitBtn = document.getElementById("btn-submit-forgot");

      if (alertDiv) { alertDiv.style.display = "none"; alertDiv.textContent = ""; }
      if (submitBtn) { submitBtn.disabled = true; submitBtn.innerHTML = `<span>Vérification…</span>`; }

      try {
        const res = await this.fetchApi("/api/auth/forgot-password", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email })
        });

        this.showToast(res.message || "Code de vérification envoyé !");
        document.getElementById("reset-email").value = email;
        const resetSub = document.getElementById("reset-view-subtitle");
        if (resetSub) {
          resetSub.textContent = `Un code de vérification à 6 chiffres a été envoyé à ${email}.`;
        }
        this.switchAuthView("reset");
      } catch (err) {
        if (alertDiv) {
          alertDiv.className = "auth-alert auth-alert-danger";
          alertDiv.textContent = err.message || "Aucun compte n'est associé à cette adresse e-mail.";
          alertDiv.style.display = "block";
        }
      } finally {
        if (submitBtn) {
          submitBtn.disabled = false;
          submitBtn.innerHTML = `<span>Envoyer le code de vérification</span><span class="auth-btn-arrow">→</span>`;
        }
      }
    });

    // 4. Soumission Réinitialisation (Code + Nouveau mot de passe)
    document.getElementById("form-auth-reset")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const email = document.getElementById("reset-email")?.value?.trim();
      const code = document.getElementById("reset-code")?.value?.trim();
      const newPassword = document.getElementById("reset-new-password")?.value;
      const confirmPassword = document.getElementById("reset-confirm-password")?.value;
      const alertDiv = document.getElementById("auth-reset-alert");
      const submitBtn = document.getElementById("btn-submit-reset");

      if (newPassword !== confirmPassword) {
        if (alertDiv) {
          alertDiv.className = "auth-alert auth-alert-danger";
          alertDiv.textContent = "Les deux mots de passe ne correspondent pas.";
          alertDiv.style.display = "block";
        }
        return;
      }

      if (newPassword.length < 8) {
        if (alertDiv) {
          alertDiv.className = "auth-alert auth-alert-danger";
          alertDiv.textContent = "Le mot de passe doit comporter au moins 8 caractères.";
          alertDiv.style.display = "block";
        }
        return;
      }

      if (alertDiv) { alertDiv.style.display = "none"; alertDiv.textContent = ""; }
      if (submitBtn) { submitBtn.disabled = true; submitBtn.innerHTML = `<span>Enregistrement…</span>`; }

      try {
        const res = await this.fetchApi("/api/auth/reset-password", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ email, code, newPassword })
        });

        this.showToast(res.message || "Mot de passe réinitialisé avec succès !");
        this.switchAuthView("login");
        const loginInput = document.getElementById("login-identifier");
        if (loginInput) loginInput.value = email;
        const loginPwd = document.getElementById("login-password");
        if (loginPwd) loginPwd.value = "";
      } catch (err) {
        if (alertDiv) {
          alertDiv.className = "auth-alert auth-alert-danger";
          alertDiv.textContent = err.message || "Code invalide ou expiré.";
          alertDiv.style.display = "block";
        }
      } finally {
        if (submitBtn) {
          submitBtn.disabled = false;
          submitBtn.innerHTML = `<span>Enregistrer et se connecter</span><span class="auth-btn-arrow">✓</span>`;
        }
      }
    });

    // 5. Toggles de visibilité du mot de passe
    document.getElementById("btn-toggle-login-pwd")?.addEventListener("click", () => {
      const input = document.getElementById("login-password");
      if (!input) return;
      input.type = input.type === "password" ? "text" : "password";
    });
    document.getElementById("btn-toggle-reset-pwd")?.addEventListener("click", () => {
      const input = document.getElementById("reset-new-password");
      if (!input) return;
      input.type = input.type === "password" ? "text" : "password";
    });

    // 6. Gestion du Profil & Déconnexion
    document.getElementById("user-profile-btn")?.addEventListener("click", () => {
      this.openModal("modal-user-profile");
    });
    document.getElementById("close-modal-user-profile")?.addEventListener("click", () => {
      this.closeModal("modal-user-profile");
    });
    document.getElementById("btn-user-logout")?.addEventListener("click", async () => {
      if (confirm("Voulez-vous vraiment vous déconnecter ?")) {
        try {
          await this.fetchApi("/api/auth/logout", { method: "POST" });
        } catch {}
        this.state.user = null;
        this.closeModal("modal-user-profile");
        this.showAuthScreen();
        this.showToast("Déconnexion réussie.");
      }
    });

    // 7. Modification du mot de passe dans l'app
    document.getElementById("btn-open-change-password")?.addEventListener("click", () => {
      this.closeModal("modal-user-profile");
      document.getElementById("form-change-password").reset();
      this.openModal("modal-change-password");
    });
    document.getElementById("close-modal-change-password")?.addEventListener("click", () => {
      this.closeModal("modal-change-password");
    });
    document.getElementById("btn-cancel-change-pwd")?.addEventListener("click", () => {
      this.closeModal("modal-change-password");
    });
    document.getElementById("form-change-password")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const currentPassword = document.getElementById("change-pwd-current")?.value;
      const newPassword = document.getElementById("change-pwd-new")?.value;
      const confirmPassword = document.getElementById("change-pwd-confirm")?.value;

      if (newPassword !== confirmPassword) {
        this.showToast("Les deux nouveaux mots de passe ne correspondent pas.", "error");
        return;
      }

      try {
        const res = await this.fetchApi("/api/auth/change-password", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ currentPassword, newPassword })
        });

        this.closeModal("modal-change-password");
        this.showToast(res.message || "Mot de passe modifié avec succès !");
      } catch (err) {
        this.showToast(err.message || "Erreur lors de la modification du mot de passe.", "error");
      }
    });
  },

  /* ------------------------------------------------------------------------
   * 0. PARAMÈTRES ENTREPRISE & PERSONNALISATION (WHITE-LABEL VIA API)
   * ------------------------------------------------------------------------ */
  async loadCompanySettings() {
    try {
      const settings = await this.fetchApi("/api/settings", {
        credentials: "include"
      });

      if (settings) {
        this.state.settings = {
          ...DEFAULT_COMPANY_SETTINGS,
          ...settings
        };
      } else {
        this.state.settings = { ...DEFAULT_COMPANY_SETTINGS };
      }
    } catch (e) {
      console.warn("Impossible de charger les paramètres depuis l'API, utilisation des valeurs par défaut", e);
      this.state.settings = { ...DEFAULT_COMPANY_SETTINGS };
    }
    this.applyCompanySettings();
  },

  async saveCompanySettings() {
    try {
      const payload = {
        companyName: this.state.settings.companyName,
        companySlogan: this.state.settings.companySlogan,
        companyEmail: this.state.settings.companyEmail,
        companyPhone: this.state.settings.companyPhone,
        companyAddress: this.state.settings.companyAddress,
        primaryColor: this.state.settings.primaryColor,
        themeMode: this.state.settings.themeMode,
        agences: this.state.settings.agences || [],
        defaultHours: parseInt(this.state.settings.defaultHours) || 40,
        defaultSla: parseInt(this.state.settings.defaultSla) || 24,
        defaultCurrency: this.state.settings.defaultCurrency || "MAD",
        defaultVisiteDuration: parseInt(this.state.settings.defaultVisiteDuration) || 120
      };

      const result = await this.fetchApi("/api/settings", {
        method: "PUT",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include",
        body: JSON.stringify(payload)
      });

      if (result) {
        this.state.settings = {
          ...DEFAULT_COMPANY_SETTINGS,
          ...result
        };
      }

      this.applyCompanySettings();
      this.showToast("Paramètres et personnalisation enregistrés avec succès !");
    } catch (e) {
      console.error("Erreur lors de la sauvegarde des paramètres:", e);
      this.showToast(e.message || "Erreur lors de l'enregistrement des paramètres.", "error");
    }
  },

  applyCompanySettings() {
    const s = this.state.settings;

    // 1. Textes & Titres
    const brandNameEl = document.getElementById("sidebar-brand-name");
    const brandSubEl = document.getElementById("sidebar-brand-sub");
    const metaTitleEl = document.getElementById("app-meta-title");
    const techPanelHeading = document.getElementById("techniciens-panel-heading");

    if (brandNameEl) brandNameEl.textContent = s.companyName || "TechnoVIS";
    if (brandSubEl) brandSubEl.textContent = s.companySlogan || "Plateforme Maintenance";
    if (metaTitleEl) metaTitleEl.textContent = `${s.companyName || 'TechnoVIS'} — Planification & Maintenance`;
    if (techPanelHeading) techPanelHeading.textContent = `Gestion de l'Équipe des Techniciens ${s.companyName ? '(' + s.companyName + ')' : ''}`;

    // 2. Thème de couleur primaire
    const color = s.primaryColor || "#0d9488";
    document.documentElement.style.setProperty("--accent", color);
    document.documentElement.style.setProperty("--primary", color);

    // 3. Mode Clair / Sombre
    document.documentElement.setAttribute("data-theme", s.themeMode || "light");

    // 4. Mettre à jour les listes d'agences
    this.populateAgencesDropdowns();
  },

  populateAgencesDropdowns() {
    const agences = this.state.settings.agences || DEFAULT_COMPANY_SETTINGS.agences;

    // Filtre Base Technicien
    const filterBase = document.getElementById("filter-base-technicien");
    if (filterBase) {
      const currentVal = filterBase.value;
      filterBase.innerHTML = `<option value="">Toutes les bases</option>`;
      agences.forEach(a => {
        const opt = document.createElement("option");
        opt.value = a;
        opt.textContent = a;
        filterBase.appendChild(opt);
      });
      filterBase.value = currentVal;
    }

    // Modal Technicien Base
    const techBase = document.getElementById("form-technicien-base");
    if (techBase) {
      const currentVal = techBase.value;
      techBase.innerHTML = "";
      agences.forEach(a => {
        const opt = document.createElement("option");
        opt.value = a;
        opt.textContent = a;
        techBase.appendChild(opt);
      });
      if (currentVal && agences.includes(currentVal)) {
        techBase.value = currentVal;
      }
    }
  },

  renderSettings() {
    const s = this.state.settings;

    // Champs texte
    document.getElementById("set-company-name").value = s.companyName || "";
    document.getElementById("set-company-slogan").value = s.companySlogan || "";
    document.getElementById("set-company-email").value = s.companyEmail || "";
    document.getElementById("set-company-phone").value = s.companyPhone || "";
    document.getElementById("set-company-address").value = s.companyAddress || "";

    document.getElementById("set-custom-color").value = s.primaryColor || "#0d9488";
    document.getElementById("set-color-picker").value = s.primaryColor || "#0d9488";
    document.getElementById("set-app-theme-mode").value = s.themeMode || "light";

    document.getElementById("set-default-hours").value = s.defaultHours || 40;
    document.getElementById("set-default-sla").value = s.defaultSla || 24;
    document.getElementById("set-default-currency").value = s.defaultCurrency || "MAD";
    document.getElementById("set-visite-duree-default").value = s.defaultVisiteDuration || 120;

    // Palette active
    document.querySelectorAll(".color-preset-btn").forEach(btn => {
      btn.classList.toggle("active", btn.getAttribute("data-color").toLowerCase() === (s.primaryColor || "").toLowerCase());
    });

    // Tags des agences
    this.renderAgencesTags();
  },

  renderAgencesTags() {
    const container = document.getElementById("settings-agences-tags");
    if (!container) return;
    container.innerHTML = "";

    const agences = this.state.settings.agences || [];
    if (agences.length === 0) {
      container.innerHTML = `<span style="color:var(--text-muted); font-size:0.8rem;">Aucune base enregistrée. Ajoutez-en une ci-dessous.</span>`;
      return;
    }

    agences.forEach((a, idx) => {
      const tag = document.createElement("span");
      tag.className = "agence-tag";
      tag.innerHTML = `
        <span>📍 ${a}</span>
        <span class="btn-remove-agence" onclick="App.removeAgence(${idx})" title="Supprimer">×</span>
      `;
      container.appendChild(tag);
    });
  },

  addAgence() {
    const input = document.getElementById("input-new-agence");
    const val = input?.value?.trim();
    if (!val) return;

    if (!this.state.settings.agences) this.state.settings.agences = [];
    if (!this.state.settings.agences.includes(val)) {
      this.state.settings.agences.push(val);
      this.renderAgencesTags();
      input.value = "";
    }
  },

  removeAgence(index) {
    if (this.state.settings.agences && this.state.settings.agences[index]) {
      this.state.settings.agences.splice(index, 1);
      this.renderAgencesTags();
    }
  },

  /* ------------------------------------------------------------------------
   * 1. COMMUNICATION API REST (ASP.NET Core)
   * ------------------------------------------------------------------------ */
  async fetchApi(endpoint, options = {}) {
    try {
      const defaultOptions = {
        credentials: "include",
        headers: {
          "Accept": "application/json"
        }
      };

      const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
          ...defaultOptions.headers,
          ...(options.headers || {})
        }
      };

      const response = await fetch(endpoint, mergedOptions);

      if (!response.ok) {
        let errBody = null;
        try { errBody = await response.json(); } catch {}

        const errorMessage = errBody?.message || errBody?.error || `Erreur HTTP ${response.status}`;

        if (response.status === 401) {
          this.setOnlineStatus(false);
          console.warn(`[401 Non Authentifié] Session expirée ou non connectée (${endpoint})`);
          if (this.state.user && !endpoint.includes("/api/auth/login") && !endpoint.includes("/api/auth/me")) {
            this.state.user = null;
            this.showAuthScreen();
            this.showToast("Votre session a expiré. Veuillez vous reconnecter.", "error");
          }
          throw new Error(errorMessage || "Session expirée. Veuillez vous reconnecter.");
        }

        if (response.status === 403) {
          console.warn(`[403 Accès Refusé] Permissions insuffisantes (${endpoint})`);
          this.showToast("Accès refusé : vous n'avez pas les droits nécessaires.", "error");
          throw new Error(errorMessage || "Accès refusé.");
        }

        if (response.status === 404) {
          throw new Error(errorMessage || "Ressource introuvable.");
        }

        if (response.status >= 500) {
          this.setOnlineStatus(false);
          throw new Error(errorMessage || "Erreur interne du serveur.");
        }

        throw new Error(errorMessage);
      }

      this.setOnlineStatus(true);

      if (response.status === 204) {
        return null;
      }

      const contentType = response.headers.get("content-type");
      if (contentType && contentType.includes("application/json")) {
        const data = await response.json();
        if (data && !Array.isArray(data) && Array.isArray(data.value)) {
          return data.value;
        }
        return data;
      }

      return await response.text();
    } catch (error) {
      console.warn(`Erreur API (${endpoint}):`, error.message || error);
      throw error;
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
      text.textContent = "Mode Hors-Ligne";
    }
  },

  async loadAllData() {
    try {
      const results = await Promise.allSettled([
        this.fetchApi("/api/dashboard/stats"),
        this.fetchApi("/api/visites"),
        this.fetchApi("/api/equipements"),
        this.fetchApi("/api/techniciens"),
        this.fetchApi("/api/techniciens/specialites"),
        this.fetchApi("/api/clients"),
        this.fetchApi("/api/marches")
      ]);

      const [statsRes, visitesRes, equipementsRes, techniciensRes, specialitesRes, clientsRes, marchesRes] = results;

      const statsData = statsRes.status === "fulfilled" ? statsRes.value : null;
      const visitesData = visitesRes.status === "fulfilled" ? visitesRes.value : [];
      const equipementsData = equipementsRes.status === "fulfilled" ? equipementsRes.value : [];
      const techniciensData = techniciensRes.status === "fulfilled" ? techniciensRes.value : [];
      const specialitesData = specialitesRes.status === "fulfilled" ? specialitesRes.value : [];
      const clientsData = clientsRes.status === "fulfilled" ? clientsRes.value : [];
      const marchesData = marchesRes.status === "fulfilled" ? marchesRes.value : [];

      this.state.stats = statsData || this.calculateFallbackStats(visitesData, equipementsData);
      this.state.visites = visitesData || [];
      this.state.equipements = equipementsData || [];
      this.state.techniciens = techniciensData || [];
      this.state.specialites = specialitesData || [];
      this.state.clients = clientsData || [];
      this.state.marches = marchesData || [];

      this.renderCurrentTab();
      this.populateActiveTechViewSelect();
    } catch (e) {
      console.error("Erreur lors du chargement des données:", e);
      this.showToast("Erreur lors du chargement des données depuis l'API.", "error");
    }
  },

  calculateFallbackStats(visites, equipements) {
    const v = visites || [];
    const eq = equipements || [];
    const planifiees = v.filter(x => x.statut === "Planifiée").length;
    const enRetard = v.filter(x => x.statut === "En retard").length;
    const validees = v.filter(x => x.statut === "Validée").length;
    const critiques = eq.filter(x => (x.scoreRisque || 0) >= 70).length;
    const conformite = v.length > 0 ? Math.round((validees / v.length) * 100) : 100;

    return {
      totalVisites: v.length,
      visitesPlanifiees: planifiees,
      visitesEnRetard: enRetard,
      visitesValidees: validees,
      totalEquipements: eq.length,
      equipementsCritiques: critiques,
      tauxConformite: conformite,
      alertesUrgent: v.filter(x => x.statut === "En retard" || (x.scorePriorite || 0) >= 80).slice(0, 5)
    };
  },

  /* ------------------------------------------------------------------------
   * 2. ROUTEUR & ÉCOUTEURS D'ÉVÉNEMENTS
   * ------------------------------------------------------------------------ */
  setupEventListeners() {
    // Navigation onglets
    document.querySelectorAll(".sidebar-nav .nav-item").forEach(item => {
      item.addEventListener("click", (e) => {
        e.preventDefault();
        const tab = item.getAttribute("data-tab");
        this.switchTab(tab);
      });
    });

    // Réduction du panneau latéral (Toggle & Persistence)
    const btnToggle = document.getElementById("btn-toggle-sidebar");
    if (btnToggle) {
      btnToggle.addEventListener("click", () => {
        document.body.classList.toggle("sidebar-collapsed");
        const isCollapsed = document.body.classList.contains("sidebar-collapsed");
        localStorage.setItem("technovis_sidebar_collapsed", isCollapsed ? "1" : "0");
      });
    }

    // Bouton Actualiser
    const btnRefresh = document.getElementById("btn-refresh-data");
    if (btnRefresh) {
      btnRefresh.addEventListener("click", () => {
        this.showToast("Actualisation des données depuis SQL Server...");
        this.loadAllData();
      });
    }

    // Bouton Réinitialiser Données (Header)
    const btnResetDb = document.getElementById("btn-reset-db");
    if (btnResetDb) {
      btnResetDb.addEventListener("click", () => this.handleResetDatabase());
    }

    // Modal Visite
    document.getElementById("btn-open-modal-visite")?.addEventListener("click", () => this.openPlanifierVisiteModal());
    document.getElementById("close-modal-visite")?.addEventListener("click", () => this.closeModal("modal-visite"));
    document.getElementById("btn-cancel-visite")?.addEventListener("click", () => this.closeModal("modal-visite"));
    document.getElementById("form-new-visite")?.addEventListener("submit", (e) => this.handleCreateVisite(e));

    // Déclencheurs dynamiques du formulaire de planification
    const eqSelect = document.getElementById("form-visite-equipement");
    const dateInput = document.getElementById("form-visite-date");
    const dureeSelect = document.getElementById("form-visite-duree");
    const typeSelect = document.getElementById("form-visite-type");

    eqSelect?.addEventListener("change", () => this.updateTechnicienRecommendations());
    dateInput?.addEventListener("change", () => this.updateTechnicienRecommendations());
    dureeSelect?.addEventListener("change", () => this.updateTechnicienRecommendations());

    typeSelect?.addEventListener("change", (e) => {
      const isAutre = e.target.value === "Autre";
      const boxAutre = document.getElementById("box-visite-autre");
      const inputLibelle = document.getElementById("form-visite-autre-libelle");
      if (boxAutre) boxAutre.style.display = isAutre ? "block" : "none";
      if (inputLibelle) inputLibelle.required = isAutre;
      this.updateTechnicienRecommendations();
    });

    // Modal Équipements
    document.getElementById("btn-open-modal-equipement")?.addEventListener("click", () => this.openEquipementModal());
    document.getElementById("close-modal-equipement")?.addEventListener("click", () => this.closeModal("modal-equipement"));
    document.getElementById("btn-cancel-equipement")?.addEventListener("click", () => this.closeModal("modal-equipement"));
    document.getElementById("form-new-equipement")?.addEventListener("submit", (e) => this.handleSaveEquipement(e));

    // Filtres Équipements
    document.getElementById("search-equipements")?.addEventListener("input", () => this.renderEquipements());
    document.getElementById("filter-categorie-equipement")?.addEventListener("change", () => this.renderEquipements());
    document.getElementById("filter-risque-equipement")?.addEventListener("change", () => this.renderEquipements());
    document.getElementById("filter-statut-equipement")?.addEventListener("change", () => this.renderEquipements());

    // Import Excel Équipements
    document.getElementById("btn-open-modal-import-equipements")?.addEventListener("click", () => this.openImportEquipementsModal());
    document.getElementById("close-modal-import-equipements")?.addEventListener("click", () => this.closeModal("modal-import-equipements"));
    document.getElementById("btn-cancel-import-eq")?.addEventListener("click", () => this.closeModal("modal-import-equipements"));
    document.getElementById("input-excel-equipements")?.addEventListener("change", (e) => {
      document.getElementById("btn-preview-excel-eq").disabled = !e.target.files.length;
    });
    document.getElementById("btn-preview-excel-eq")?.addEventListener("click", () => this.handleEquipementsExcelPreview());
    document.getElementById("btn-back-import-eq")?.addEventListener("click", () => this.showImportEqStep(1));
    document.getElementById("btn-confirm-import-eq")?.addEventListener("click", () => this.handleEquipementsExcelConfirm());

    // Modal Techniciens
    document.getElementById("btn-open-modal-technicien")?.addEventListener("click", () => this.openTechnicienModal());
    document.getElementById("close-modal-technicien")?.addEventListener("click", () => this.closeModal("modal-technicien"));
    document.getElementById("btn-cancel-technicien")?.addEventListener("click", () => this.closeModal("modal-technicien"));
    document.getElementById("form-new-technicien")?.addEventListener("submit", (e) => this.handleSaveTechnicien(e));

    // Filtres Techniciens
    document.getElementById("search-techniciens")?.addEventListener("input", () => this.renderTechniciens());
    document.getElementById("filter-base-technicien")?.addEventListener("change", () => this.renderTechniciens());
    document.getElementById("filter-statut-technicien")?.addEventListener("change", () => this.renderTechniciens());

    // Modal Rapport / PV
    document.getElementById("close-modal-rapport")?.addEventListener("click", () => this.closeModal("modal-rapport"));
    document.getElementById("btn-cancel-rapport")?.addEventListener("click", () => this.closeModal("modal-rapport"));
    document.getElementById("form-rapport-technique")?.addEventListener("submit", (e) => this.handleUpdateRapport(e));
    
    // Téléchargement direct du PV PDF avec nom de fichier et extension propres
    document.getElementById("btn-export-pv")?.addEventListener("click", () => {
      const id = document.getElementById("form-rapport-id").value;
      if (id) {
        const visite = this.state.visites.find(v => v.id === parseInt(id));
        const refName = visite ? visite.reference : `VIS-${id}`;
        const link = document.createElement("a");
        link.href = `/api/visites/${id}/pv-pdf`;
        link.download = `PV_${refName}.pdf`;
        document.body.appendChild(link);
        link.click();
        link.remove();
      }
    });

    // Modal Marchés
    document.getElementById("btn-open-modal-marche")?.addEventListener("click", () => {
      this.populateClientDropdown();
      this.openModal("modal-marche");
    });
    document.getElementById("close-modal-marche")?.addEventListener("click", () => this.closeModal("modal-marche"));
    document.getElementById("btn-cancel-marche")?.addEventListener("click", () => this.closeModal("modal-marche"));
    document.getElementById("form-new-marche")?.addEventListener("submit", (e) => this.handleCreateMarche(e));

    // Import Excel Marchés
    document.getElementById("btn-open-modal-import-excel")?.addEventListener("click", () => this.openExcelImportModal());
    document.getElementById("close-modal-import-excel")?.addEventListener("click", () => this.closeModal("modal-import-excel"));
    document.getElementById("btn-cancel-import-excel")?.addEventListener("click", () => this.closeModal("modal-import-excel"));
    document.getElementById("input-excel-file")?.addEventListener("change", (e) => {
      document.getElementById("btn-preview-excel").disabled = !e.target.files.length;
    });
    document.getElementById("btn-preview-excel")?.addEventListener("click", () => this.handleExcelPreview());
    document.getElementById("btn-back-import")?.addEventListener("click", () => this.showImportStep(1));
    document.getElementById("btn-confirm-import")?.addEventListener("click", () => this.handleExcelConfirm());

    // Centre d'Exportation
    document.querySelectorAll("[data-export-source]").forEach(btn => {
      btn.addEventListener("click", (e) => {
        const source = e.currentTarget.getAttribute("data-export-source");
        this.openExportModal(source);
      });
    });
    document.getElementById("close-modal-export")?.addEventListener("click", () => this.closeModal("modal-export-preview"));
    document.getElementById("btn-cancel-export")?.addEventListener("click", () => this.closeModal("modal-export-preview"));
    document.getElementById("btn-refresh-export-preview")?.addEventListener("click", () => this.refreshExportPreview());
    document.getElementById("export-source")?.addEventListener("change", () => this.refreshExportPreview());
    document.getElementById("export-format")?.addEventListener("change", () => this.updateExportFormatDesc());
    document.getElementById("btn-do-export")?.addEventListener("click", () => this.doExport());

    // Filtre Planning
    document.getElementById("filter-statut-visite")?.addEventListener("change", () => this.renderPlanning());

    // Navigation Calendrier
    document.getElementById("cal-prev")?.addEventListener("click", () => {
      this.state.currentMonth.setMonth(this.state.currentMonth.getMonth() - 1);
      this.renderCalendar();
    });
    document.getElementById("cal-next")?.addEventListener("click", () => {
      this.state.currentMonth.setMonth(this.state.currentMonth.getMonth() + 1);
      this.renderCalendar();
    });
    document.getElementById("cal-today")?.addEventListener("click", () => {
      this.state.currentMonth = new Date();
      this.renderCalendar();
    });

    // Filtre Mode Terrain
    document.getElementById("select-active-tech-view")?.addEventListener("change", () => this.renderTechnicien());

    // Écouteurs Panneau Paramètres
    document.querySelectorAll(".color-preset-btn").forEach(btn => {
      btn.addEventListener("click", (e) => {
        const color = e.currentTarget.getAttribute("data-color");
        document.getElementById("set-custom-color").value = color;
        document.getElementById("set-color-picker").value = color;
        document.querySelectorAll(".color-preset-btn").forEach(b => b.classList.remove("active"));
        e.currentTarget.classList.add("active");
        this.state.settings.primaryColor = color;
        this.applyCompanySettings();
      });
    });

    document.getElementById("set-color-picker")?.addEventListener("input", (e) => {
      const color = e.target.value;
      document.getElementById("set-custom-color").value = color;
      this.state.settings.primaryColor = color;
      this.applyCompanySettings();
    });

    document.getElementById("set-custom-color")?.addEventListener("change", (e) => {
      const color = e.target.value;
      document.getElementById("set-color-picker").value = color;
      this.state.settings.primaryColor = color;
      this.applyCompanySettings();
    });

    document.getElementById("set-app-theme-mode")?.addEventListener("change", (e) => {
      this.state.settings.themeMode = e.target.value;
      this.applyCompanySettings();
    });

    document.getElementById("btn-add-agence")?.addEventListener("click", () => this.addAgence());
    document.getElementById("input-new-agence")?.addEventListener("keypress", (e) => {
      if (e.key === "Enter") { e.preventDefault(); this.addAgence(); }
    });

    document.getElementById("btn-save-settings")?.addEventListener("click", () => {
      this.state.settings.companyName = document.getElementById("set-company-name").value.trim() || "TechnoVIS";
      this.state.settings.companySlogan = document.getElementById("set-company-slogan").value.trim();
      this.state.settings.companyEmail = document.getElementById("set-company-email").value.trim();
      this.state.settings.companyPhone = document.getElementById("set-company-phone").value.trim();
      this.state.settings.companyAddress = document.getElementById("set-company-address").value.trim();
      this.state.settings.primaryColor = document.getElementById("set-custom-color").value.trim() || "#0d9488";
      this.state.settings.themeMode = document.getElementById("set-app-theme-mode").value;
      this.state.settings.defaultHours = parseInt(document.getElementById("set-default-hours").value) || 40;
      this.state.settings.defaultSla = parseInt(document.getElementById("set-default-sla").value) || 24;
      this.state.settings.defaultCurrency = document.getElementById("set-default-currency").value;
      this.state.settings.defaultVisiteDuration = parseInt(document.getElementById("set-visite-duree-default").value) || 120;

      this.saveCompanySettings();
    });

    document.getElementById("btn-reset-settings")?.addEventListener("click", () => {
      if (confirm("Réinitialiser tous les paramètres aux valeurs d'origine ?")) {
        this.state.settings = { ...DEFAULT_COMPANY_SETTINGS };
        this.saveCompanySettings();
        this.renderSettings();
      }
    });

    document.getElementById("btn-export-settings-json")?.addEventListener("click", () => {
      const dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(this.state.settings, null, 2));
      const dlAnchor = document.createElement("a");
      dlAnchor.setAttribute("href", dataStr);
      dlAnchor.setAttribute("download", `technovis_config_${new Date().toISOString().slice(0,10)}.json`);
      dlAnchor.click();
    });
  },

  /* ------------------------------------------------------------------------
   * 2b. VOLET LATÉRAL KPI (SIDE DRAWER)
   * ------------------------------------------------------------------------ */
  setupKpiDrawerListeners() {
    // Écouteur sur chaque carte KPI cliquable
    document.querySelectorAll(".metric-card-clickable").forEach(card => {
      card.addEventListener("click", () => {
        const kpi = card.getAttribute("data-kpi");
        this.openKpiDrawer(kpi);
      });
    });

    // Fermeture du drawer
    document.getElementById("kpi-drawer-close")?.addEventListener("click", () => this.closeKpiDrawer());
    document.getElementById("kpi-drawer-backdrop")?.addEventListener("click", () => this.closeKpiDrawer());

    // Touche Echap pour fermer le drawer
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") this.closeKpiDrawer();
    });
  },

  openKpiDrawer(kpiType) {
    const drawer = document.getElementById("kpi-drawer");
    const backdrop = document.getElementById("kpi-drawer-backdrop");
    const titleEl = document.getElementById("kpi-drawer-title");
    const subEl = document.getElementById("kpi-drawer-subtitle");
    const bodyEl = document.getElementById("kpi-drawer-body");

    if (!drawer || !backdrop || !titleEl || !bodyEl) return;

    bodyEl.innerHTML = "";

    switch (kpiType) {
      case "total":
        titleEl.textContent = "Total des Visites";
        subEl.textContent = `${this.state.visites.length} intervention(s) répertoriée(s)`;
        this.renderKpiVisitesList(this.state.visites, bodyEl);
        break;

      case "planifiees":
        const planifiees = this.state.visites.filter(v => v.statut === "Planifiée");
        titleEl.textContent = "Visites Planifiées";
        subEl.textContent = `${planifiees.length} intervention(s) à venir`;
        this.renderKpiVisitesList(planifiees, bodyEl);
        break;

      case "retard":
        const retards = this.state.visites.filter(v => v.statut === "En retard" || (v.scorePriorite || 0) >= 80);
        titleEl.textContent = "Alertes & Interventions en Retard";
        subEl.textContent = `${retards.length} intervention(s) prioritaire(s)`;
        this.renderKpiVisitesList(retards, bodyEl);
        break;

      case "critiques":
        const critiques = this.state.equipements.filter(e => (e.scoreRisque || 0) >= 70 || (e.criticite || 0) >= 4);
        titleEl.textContent = "Équipements Critiques";
        subEl.textContent = `${critiques.length} équipement(s) à risque élevé`;
        this.renderKpiEquipementsList(critiques, bodyEl);
        break;

      case "validees":
        const validees = this.state.visites.filter(v => v.statut === "Validée");
        titleEl.textContent = "Visites Validées & Taux de Conformité";
        subEl.textContent = `${validees.length} intervention(s) terminée(s) et conformes`;
        this.renderKpiVisitesList(validees, bodyEl);
        break;

      default:
        titleEl.textContent = "Détail";
        subEl.textContent = "";
        bodyEl.innerHTML = `<div style="padding:20px; text-align:center; color:var(--text-muted)">Aucun détail disponible.</div>`;
    }

    drawer.classList.add("open");
    backdrop.classList.add("open");
  },

  closeKpiDrawer() {
    document.getElementById("kpi-drawer")?.classList.remove("open");
    document.getElementById("kpi-drawer-backdrop")?.classList.remove("open");
  },

  renderKpiVisitesList(list, container) {
    if (!list || list.length === 0) {
      container.innerHTML = `<div style="padding:32px 24px; text-align:center; color:var(--text-muted);">Aucune intervention dans cette catégorie.</div>`;
      return;
    }

    list.forEach(v => {
      const item = document.createElement("div");
      item.className = "drawer-item";
      const dateFormatted = new Date(v.datePrevue).toLocaleDateString("fr-FR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
      const typeDisplay = v.typeVisiteAffiche || (v.typeVisite === "Autre" && v.typeVisiteAutre ? `Autre (${v.typeVisiteAutre})` : v.typeVisite);

      item.innerHTML = `
        <div style="display:flex; justify-content:space-between; align-items:center;">
          <strong>${v.reference}</strong>
          <span class="badge ${this.getBadgeClass(v.statut)}">${v.statut}</span>
        </div>
        <div style="font-weight:600; font-size:0.92rem; color:var(--text-primary);">${v.equipementNom}</div>
        <div style="font-size:0.8rem; color:var(--text-secondary);">
          🏢 ${v.clientNom || 'Client N/A'} • 📍 ${v.siteNom || 'Site N/A'}
        </div>
        <div style="font-size:0.78rem; color:var(--text-muted); display:flex; justify-content:space-between; align-items:center; margin-top:4px;">
          <span>👷 ${v.technicienNom || 'Non assigné'}</span>
          <span>📅 ${dateFormatted}</span>
        </div>
        <div style="margin-top:8px; display:flex; gap:8px;">
          <button class="btn btn-secondary btn-sm" onclick="App.closeKpiDrawer(); App.openRapportModal(${v.id});" style="font-size:0.75rem; padding:4px 10px;">
            📄 Ouvrir Fiche
          </button>
        </div>
      `;
      container.appendChild(item);
    });
  },

  renderKpiEquipementsList(list, container) {
    if (!list || list.length === 0) {
      container.innerHTML = `<div style="padding:32px 24px; text-align:center; color:var(--text-muted);">Aucun équipement critique.</div>`;
      return;
    }

    list.forEach(e => {
      const item = document.createElement("div");
      item.className = "drawer-item";
      const scoreRisque = e.scoreRisque || 15;
      const riskClass = scoreRisque >= 70 ? 'badge-retard' : 'badge-planifiee';

      item.innerHTML = `
        <div style="display:flex; justify-content:space-between; align-items:center;">
          <strong>${e.serialNumber}</strong>
          <span class="badge ${riskClass}">Risque ${scoreRisque}/100</span>
        </div>
        <div style="font-weight:600; font-size:0.92rem;">${e.nom}</div>
        <div style="font-size:0.8rem; color:var(--text-secondary);">
          Catégorie : <span class="spec-badge">${e.categorie}</span> • Criticité : <strong>${e.criticite || 3}/5</strong>
        </div>
        <div style="font-size:0.78rem; color:var(--text-muted);">
          🏢 ${e.clientNom || 'Client N/A'} — 📍 ${e.siteNom || 'Site N/A'}
        </div>
        <div style="margin-top:8px;">
          <button class="btn btn-primary btn-sm" onclick="App.closeKpiDrawer(); App.planifierPourEquipement(${e.id});" style="font-size:0.75rem; padding:4px 10px;">
            📅 Planifier Visite Immédiate
          </button>
        </div>
      `;
      container.appendChild(item);
    });
  },

  /* ------------------------------------------------------------------------
   * 2c. RÉINITIALISATION COMPLÈTE DES DONNÉES (RESET DATABASE)
   * ------------------------------------------------------------------------ */
  async handleResetDatabase() {
    const msg = "⚠️ Voulez-vous vraiment vider toutes les tables ?\n\nToutes les données (visites, équipements, techniciens, marchés, clients et sites) ainsi que vos imports Excel seront définitivement supprimés. Les tableaux seront remis à zéro.";
    if (!confirm(msg)) return;

    this.showToast("Vidage des tables en cours...");

    try {
      const res = await this.fetchApi("/api/dashboard/reset-data", {
        method: "POST",
        headers: { "Content-Type": "application/json" }
      });

      if (res && res.message) {
        this.showToast(res.message);
      } else {
        this.showToast("Toutes les tables ont été vidées avec succès !");
      }

      // Recharger immédiatement toutes les données
      await this.loadAllData();
    } catch (e) {
      console.error(e);
      this.showToast("Erreur lors du vidage des tables.", "error");
    }
  },

  switchTab(tabId) {
    this.state.currentTab = tabId;

    document.querySelectorAll(".sidebar-nav .nav-item").forEach(item => {
      item.classList.toggle("active", item.getAttribute("data-tab") === tabId);
    });

    document.querySelectorAll(".content-area .tab-view").forEach(section => {
      section.classList.toggle("active", section.id === `tab-${tabId}`);
    });

    const titles = {
      dashboard: "Tableau de Bord",
      planning: "Planification & Calendrier",
      equipements: "Gestion des Équipements",
      techniciens: "Techniciens",
      clients: "Marchés & Clients",
      technicien: "Mode Terrain",
      settings: "Paramètres & Personnalisation"
    };
    document.getElementById("header-page-title").textContent = titles[tabId] || "TechnoVIS";

    this.renderCurrentTab();
  },

  renderCurrentTab() {
    switch (this.state.currentTab) {
      case "dashboard":
        this.renderDashboard();
        break;
      case "planning":
        this.renderCalendar();
        this.renderPlanning();
        break;
      case "equipements":
        this.renderEquipements();
        break;
      case "techniciens":
        this.renderTechniciens();
        break;
      case "clients":
        this.renderClients();
        break;
      case "technicien":
        this.renderTechnicien();
        break;
      case "settings":
        this.renderSettings();
        break;
    }
  },

  /* ------------------------------------------------------------------------
   * 3. TABLEAU DE BORD (DASHBOARD)
   * ------------------------------------------------------------------------ */
  renderDashboard() {
    const stats = this.state.stats;
    if (!stats) return;

    document.getElementById("kpi-total-visites").textContent = stats.totalVisites ?? 0;
    document.getElementById("kpi-visites-planifiees").textContent = stats.visitesPlanifiees ?? 0;
    document.getElementById("kpi-visites-retard").textContent = stats.visitesEnRetard ?? 0;
    document.getElementById("kpi-equipements-critiques").textContent = stats.equipementsCritiques ?? 0;
    document.getElementById("kpi-taux-conformite").textContent = `${stats.tauxConformite ?? 100}%`;

    // Graphique 1: Visites par Statut
    const ctx1 = document.getElementById("chart-visites-statut");
    if (ctx1 && typeof Chart !== "undefined") {
      if (ctx1._chartInstance) ctx1._chartInstance.destroy();
      const planifiees = stats.visitesPlanifiees ?? 0;
      const enRetard = stats.visitesEnRetard ?? 0;
      const validees = stats.visitesValidees ?? 0;

      ctx1._chartInstance = new Chart(ctx1, {
        type: "bar",
        data: {
          labels: ["Planifiées", "En Retard", "Validées"],
          datasets: [{
            data: [planifiees, enRetard, validees],
            backgroundColor: [this.state.settings.primaryColor || "#0d9488", "#e05a5a", "#34c38f"],
            borderRadius: 6
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            x: { grid: { display: false } },
            y: { beginAtZero: true, grid: { color: "#e5e5ea" }, ticks: { stepSize: 1 } }
          }
        }
      });
    }

    // Graphique 2: Risque Équipements
    const ctx2 = document.getElementById("chart-equipements-risque");
    if (ctx2 && typeof Chart !== "undefined" && this.state.equipements.length > 0) {
      if (ctx2._chartInstance) ctx2._chartInstance.destroy();
      const eqs = this.state.equipements;
      const faible = eqs.filter(e => (e.scoreRisque || 0) < 40).length;
      const moyen = eqs.filter(e => (e.scoreRisque || 0) >= 40 && (e.scoreRisque || 0) < 70).length;
      const critique = eqs.filter(e => (e.scoreRisque || 0) >= 70).length;

      ctx2._chartInstance = new Chart(ctx2, {
        type: "bar",
        data: {
          labels: ["Faible (< 40)", "Moyen (40-69)", "Critique (≥ 70)"],
          datasets: [{
            data: [faible, moyen, critique],
            backgroundColor: ["#34c38f", "#f5a623", "#e05a5a"],
            borderRadius: 6
          }]
        },
        options: {
          indexAxis: "y",
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: {
            x: { beginAtZero: true, grid: { color: "#e5e5ea" }, ticks: { stepSize: 1 } },
            y: { grid: { display: false } }
          }
        }
      });
    }

    // Alertes urgentes
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
   * 4. PLANIFICATION & CALENDRIER
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

    const days = ["Lun", "Mar", "Mer", "Jeu", "Ven", "Sam", "Dim"];
    days.forEach(d => {
      const h = document.createElement("div");
      h.className = "calendar-day-header";
      h.textContent = d;
      grid.appendChild(h);
    });

    const firstDay = new Date(year, month, 1).getDay();
    const startingDay = firstDay === 0 ? 6 : firstDay - 1;
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    for (let i = 0; i < startingDay; i++) {
      const empty = document.createElement("div");
      empty.className = "calendar-day empty";
      grid.appendChild(empty);
    }

    const today = new Date();
    for (let d = 1; d <= daysInMonth; d++) {
      const cell = document.createElement("div");
      cell.className = "calendar-day";
      if (today.getFullYear() === year && today.getMonth() === month && today.getDate() === d) {
        cell.classList.add("today");
      }

      const num = document.createElement("span");
      num.className = "day-number";
      num.textContent = d;
      cell.appendChild(num);

      const cellDateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
      const dayVisites = this.state.visites.filter(v => v.datePrevue && v.datePrevue.startsWith(cellDateStr));

      dayVisites.forEach(v => {
        const chip = document.createElement("div");
        chip.className = `event-chip ${this.getBadgeClass(v.statut)}`;
        chip.title = `${v.reference} — ${v.equipementNom} (${v.technicienNom})`;
        chip.textContent = `${v.reference} - ${v.equipementNom}`;
        chip.addEventListener("click", () => this.openRapportModal(v.id));
        cell.appendChild(chip);
      });

      grid.appendChild(cell);
    }
  },

  renderPlanning() {
    const tbody = document.getElementById("table-planning-body");
    if (!tbody) return;
    tbody.innerHTML = "";

    const filterStatut = document.getElementById("filter-statut-visite")?.value || "";
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
      const dateFormatted = new Date(v.datePrevue).toLocaleDateString("fr-FR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
      const typeDisplay = v.typeVisiteAffiche || (v.typeVisite === "Autre" && v.typeVisiteAutre ? `Autre (${v.typeVisiteAutre})` : v.typeVisite);

      tr.innerHTML = `
        <td><strong>${v.reference}</strong></td>
        <td><span class="badge badge-planifiee">${typeDisplay}</span></td>
        <td>
          <div style="font-weight:600;">${v.equipementNom}</div>
          <small style="color:var(--text-muted);">${v.equipementSerial}</small>
        </td>
        <td>
          <div>${v.clientNom || "Client N/A"}</div>
          <small style="color:var(--text-muted);">${v.siteNom || "Site N/A"}</small>
        </td>
        <td>
          <div style="font-weight:500;">${v.technicienNom}</div>
          <small style="color:var(--text-muted);">${v.technicienMatricule || ''}</small>
        </td>
        <td>${dateFormatted}</td>
        <td><span class="badge ${v.scorePriorite >= 80 ? 'badge-retard' : 'badge-planifiee'}">Prio ${v.scorePriorite}</span></td>
        <td><span class="badge ${this.getBadgeClass(v.statut)}">${v.statut}</span></td>
        <td>
          <button class="btn btn-secondary btn-sm" onclick="App.openRapportModal(${v.id})">Fiche</button>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  /* ------------------------------------------------------------------------
   * 5. GESTION DU PARC D'ÉQUIPEMENTS
   * ------------------------------------------------------------------------ */
  renderEquipements() {
    const tbody = document.getElementById("table-equipements-body");
    const badgeCount = document.getElementById("badge-total-equipements");
    if (!tbody) return;
    tbody.innerHTML = "";

    const search = document.getElementById("search-equipements")?.value?.toLowerCase().trim() || "";
    const cat = document.getElementById("filter-categorie-equipement")?.value || "";
    const risque = document.getElementById("filter-risque-equipement")?.value || "";
    const statut = document.getElementById("filter-statut-equipement")?.value || "";

    let list = this.state.equipements;

    if (search) {
      list = list.filter(e => (e.nom && e.nom.toLowerCase().includes(search)) ||
                              (e.serialNumber && e.serialNumber.toLowerCase().includes(search)) ||
                              (e.clientNom && e.clientNom.toLowerCase().includes(search)) ||
                              (e.siteNom && e.siteNom.toLowerCase().includes(search)));
    }
    if (cat) list = list.filter(e => e.categorie === cat);
    if (statut) list = list.filter(e => e.statut === statut);
    if (risque) {
      if (risque === "critique") list = list.filter(e => (e.scoreRisque || 0) >= 70);
      else if (risque === "moyen") list = list.filter(e => (e.scoreRisque || 0) >= 40 && (e.scoreRisque || 0) < 70);
      else if (risque === "faible") list = list.filter(e => (e.scoreRisque || 0) < 40);
    }

    if (badgeCount) badgeCount.textContent = `${list.length} équipement(s)`;

    if (list.length === 0) {
      tbody.innerHTML = `<tr><td colspan="9" style="text-align: center; color: var(--text-muted); padding: 2rem;">Aucun équipement ne correspond aux critères.</td></tr>`;
      return;
    }

    list.forEach(e => {
      const tr = document.createElement("tr");
      const lastVisit = e.derniereVisite ? new Date(e.derniereVisite).toLocaleDateString("fr-FR") : "—";
      const scoreRisque = e.scoreRisque || 15;
      const riskClass = scoreRisque >= 70 ? 'badge-retard' : (scoreRisque >= 40 ? 'badge-planifiee' : 'badge-validee');

      tr.innerHTML = `
        <td><strong>${e.serialNumber}</strong></td>
        <td>
          <div style="font-weight:600;">${e.nom}</div>
          <small style="color:var(--text-muted);">Santé : ${e.scoreSante || 85}%</small>
        </td>
        <td><span class="spec-badge">${e.categorie}</span></td>
        <td>
          <div style="font-weight:500;">${e.clientNom || 'Client N/A'}</div>
          <small style="color:var(--text-muted);">${e.siteNom || 'Site N/A'} (${e.siteVille || ''})</small>
        </td>
        <td>Criticité ${e.criticite || 3}/5</td>
        <td><span class="badge ${riskClass}">${scoreRisque} / 100</span></td>
        <td><span class="badge ${e.statut === 'Opérationnel' ? 'badge-validee' : 'badge-retard'}">${e.statut}</span></td>
        <td>${lastVisit}</td>
        <td>
          <div style="display:flex; gap:6px;">
            <button class="btn btn-secondary btn-sm" onclick="App.openEquipementModal(${e.id})" title="Modifier">✏️</button>
            <button class="btn btn-primary btn-sm" onclick="App.planifierPourEquipement(${e.id})" title="Planifier Visite">📅</button>
            <button class="btn btn-secondary btn-sm" onclick="App.handleDeleteEquipement(${e.id})" title="Supprimer" style="color:var(--danger);">🗑️</button>
          </div>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  planifierPourEquipement(equipementId) {
    this.openPlanifierVisiteModal(equipementId);
  },

  openEquipementModal(id = null) {
    this.populateSiteDropdown();
    const modalTitle = document.getElementById("modal-equipement-title");
    const idInput = document.getElementById("form-equipement-id");
    const serialInput = document.getElementById("form-equipement-serial");
    const nomInput = document.getElementById("form-equipement-nom");
    const catSelect = document.getElementById("form-equipement-categorie");
    const siteSelect = document.getElementById("form-equipement-site");
    const critInput = document.getElementById("form-equipement-criticite");
    const santeInput = document.getElementById("form-equipement-sante");
    const dateInput = document.getElementById("form-equipement-date");
    const statutSelect = document.getElementById("form-equipement-statut");

    if (id) {
      const eq = this.state.equipements.find(x => x.id === id);
      if (!eq) return;
      modalTitle.textContent = "Modifier l'Équipement";
      idInput.value = eq.id;
      serialInput.value = eq.serialNumber;
      serialInput.disabled = true;
      nomInput.value = eq.nom;
      catSelect.value = eq.categorie;
      siteSelect.value = eq.siteId;
      critInput.value = eq.criticite;
      santeInput.value = eq.scoreSante;
      dateInput.value = eq.dateInstallation ? eq.dateInstallation.split("T")[0] : new Date().toISOString().split("T")[0];
      statutSelect.value = eq.statut;
    } else {
      modalTitle.textContent = "Ajouter un Équipement";
      idInput.value = "";
      serialInput.value = "";
      serialInput.disabled = false;
      nomInput.value = "";
      critInput.value = 3;
      santeInput.value = 85;
      dateInput.value = new Date().toISOString().split("T")[0];
      statutSelect.value = "Opérationnel";
    }

    this.openModal("modal-equipement");
  },

  async handleSaveEquipement(e) {
    e.preventDefault();
    const id = document.getElementById("form-equipement-id").value;
    const serialNumber = document.getElementById("form-equipement-serial").value;
    const nom = document.getElementById("form-equipement-nom").value;
    const categorie = document.getElementById("form-equipement-categorie").value;
    const siteId = parseInt(document.getElementById("form-equipement-site").value);
    const criticite = parseInt(document.getElementById("form-equipement-criticite").value);
    const scoreSante = parseInt(document.getElementById("form-equipement-sante").value);
    const dateInstallation = document.getElementById("form-equipement-date").value;
    const statut = document.getElementById("form-equipement-statut").value;

    const payload = {
      id: id ? parseInt(id) : 0,
      serialNumber,
      nom,
      categorie,
      siteId,
      criticite,
      scoreSante,
      dateInstallation: new Date(dateInstallation).toISOString(),
      statut
    };

    try {
      let res;
      if (id) {
        res = await this.fetchApi(`/api/equipements/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
      } else {
        res = await this.fetchApi("/api/equipements", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
      }

      this.closeModal("modal-equipement");
      this.showToast(`Équipement ${res?.nom || nom || ''} enregistré avec succès !`);
      this.loadAllData();
    } catch (err) {
      console.error(err);
      this.showToast(err.message || "Erreur lors de l'enregistrement de l'équipement.", "error");
    }
  },

  async handleDeleteEquipement(id) {
    if (!confirm("Voulez-vous vraiment supprimer cet équipement ?")) return;
    try {
      await this.fetchApi(`/api/equipements/${id}`, { method: "DELETE" });
      this.showToast("Équipement supprimé avec succès.");
      this.loadAllData();
    } catch (err) {
      console.error(err);
      this.showToast(err.message || "Erreur lors de la suppression de l'équipement.", "error");
    }
  },

  /* ── IMPORT EXCEL ÉQUIPEMENTS ── */
  openImportEquipementsModal() {
    const fileInput = document.getElementById("input-excel-equipements");
    if (fileInput) fileInput.value = "";
    document.getElementById("btn-preview-excel-eq").disabled = true;
    const errDiv = document.getElementById("import-eq-error-msg");
    if (errDiv) { errDiv.textContent = ""; errDiv.style.display = "none"; }
    this._equipementsImportAllRows = null;
    this.showImportEqStep(1);
    this.openModal("modal-import-equipements");
  },

  showImportEqStep(step) {
    document.getElementById("import-eq-step-1").style.display = step === 1 ? "block" : "none";
    document.getElementById("import-eq-step-2").style.display = step === 2 ? "block" : "none";
  },

  async handleEquipementsExcelPreview() {
    const fileInput = document.getElementById("input-excel-equipements");
    if (!fileInput || !fileInput.files.length) return;

    const btn = document.getElementById("btn-preview-excel-eq");
    btn.textContent = "Analyse en cours…";
    btn.disabled = true;

    const formData = new FormData();
    formData.append("file", fileInput.files[0]);

    try {
      const resp = await fetch("/api/equipements/import/preview", { method: "POST", body: formData });
      const result = await resp.json();
      if (!resp.ok) {
        const errDiv = document.getElementById("import-eq-error-msg");
        errDiv.textContent = result.error || "Erreur d'analyse.";
        errDiv.style.display = "block";
        btn.textContent = "Analyser le fichier";
        btn.disabled = false;
        return;
      }

      this._equipementsImportAllRows = result.allRows;

      document.getElementById("import-eq-summary").innerHTML = `
        <strong>${result.rowCount}</strong> équipement(s) détecté(s). Aperçu des premières lignes et contrôles de cohérence :
      `;

      const tbody = document.getElementById("import-eq-preview-body");
      tbody.innerHTML = "";
      (result.preview || []).forEach(r => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td>${r.rowIndex}</td>
          <td><strong>${r.serialNumber || '—'}</strong></td>
          <td>${r.nom || '—'}</td>
          <td><span class="spec-badge">${r.categorie || '—'}</span></td>
          <td>${r.clientNom || '—'}</td>
          <td>${r.siteNom || '—'}</td>
          <td>${r.criticite}/5</td>
          <td>${r.statut}</td>
          <td style="color: ${r.parseWarning ? '#f5a623' : '#34c38f'}; font-size:0.75rem;">
            ${r.parseWarning || "✓ Valide"}
          </td>
        `;
        tbody.appendChild(tr);
      });

      btn.textContent = "Analyser le fichier";
      btn.disabled = false;
      this.showImportEqStep(2);
    } catch (e) {
      console.error(e);
      btn.textContent = "Analyser le fichier";
      btn.disabled = false;
    }
  },

  async handleEquipementsExcelConfirm() {
    if (!this._equipementsImportAllRows || !this._equipementsImportAllRows.length) return;

    const btn = document.getElementById("btn-confirm-import-eq");
    btn.textContent = "Importation dans SQL Server…";
    btn.disabled = true;

    try {
      const resp = await fetch("/api/equipements/import/confirm", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(this._equipementsImportAllRows)
      });
      const result = await resp.json();

      this.closeModal("modal-import-equipements");
      this.showToast(`Import terminé : ${result.imported} équipement(s) créés, ${result.updated} mis à jour.`);
      this.loadAllData();
    } catch (e) {
      this.showToast("Erreur lors de l'enregistrement.", "error");
    } finally {
      btn.textContent = "Confirmer l'import en base";
      btn.disabled = false;
      this._equipementsImportAllRows = null;
    }
  },

  /* ------------------------------------------------------------------------
   * 6. GESTION DES TECHNICIENS
   * ------------------------------------------------------------------------ */
  renderTechniciens() {
    const tbody = document.getElementById("table-techniciens-body");
    const badgeCount = document.getElementById("badge-total-techniciens");
    if (!tbody) return;
    tbody.innerHTML = "";

    const search = document.getElementById("search-techniciens")?.value?.toLowerCase().trim() || "";
    const base = document.getElementById("filter-base-technicien")?.value || "";
    const statut = document.getElementById("filter-statut-technicien")?.value || "";

    let list = this.state.techniciens;

    if (search) {
      list = list.filter(t => (t.nomComplet && t.nomComplet.toLowerCase().includes(search)) ||
                              (t.matricule && t.matricule.toLowerCase().includes(search)) ||
                              (t.base && t.base.toLowerCase().includes(search)) ||
                              (t.email && t.email.toLowerCase().includes(search)));
    }
    if (base) list = list.filter(t => t.base === base);
    if (statut) list = list.filter(t => t.statut === statut);

    if (badgeCount) badgeCount.textContent = `${list.length} technicien(s)`;

    if (list.length === 0) {
      tbody.innerHTML = `<tr><td colspan="8" style="text-align: center; color: var(--text-muted); padding: 2rem;">Aucun technicien trouvé.</td></tr>`;
      return;
    }

    list.forEach(t => {
      const tr = document.createElement("tr");
      const specsBadges = (t.specialites || []).map(s => `<span class="spec-badge">${s.nom}</span>`).join(" ") || `<small style="color:var(--text-muted)">Aucune</small>`;
      
      const capacite = t.heuresHebdo || this.state.settings.defaultHours || 40;
      const planifiees = t.heuresPlanifiees || 0;
      const pct = Math.min(100, Math.round((planifiees / capacite) * 100));
      const fillClass = pct >= 90 ? 'danger' : (pct >= 60 ? 'warning' : '');

      tr.innerHTML = `
        <td><strong>${t.matricule}</strong></td>
        <td>
          <div style="font-weight:600;">${t.prenom} ${t.nom}</div>
          <small style="color:var(--text-muted);">${t.email || ''} ${t.telephone ? '• ' + t.telephone : ''}</small>
        </td>
        <td><span class="badge badge-planifiee">${t.base || 'Casablanca'}</span></td>
        <td>${specsBadges}</td>
        <td>
          <div class="tech-hours-container">
            <div style="display:flex; justify-content:space-between; font-size:0.75rem;">
              <span>${planifiees}h / ${capacite}h</span>
              <span style="font-weight:600;">${pct}%</span>
            </div>
            <div class="tech-hours-bar">
              <div class="tech-hours-fill ${fillClass}" style="width: ${pct}%;"></div>
            </div>
          </div>
        </td>
        <td>
          <span class="badge ${t.disponible && t.statut === 'Actif' ? 'badge-validee' : 'badge-retard'}">
            ${t.disponible && t.statut === 'Actif' ? '🟢 Disponible' : '🔴 ' + t.statut}
          </span>
        </td>
        <td><strong>${t.visitesActives ?? 0}</strong> active(s)</td>
        <td>
          <div style="display:flex; gap:6px;">
            <button class="btn btn-secondary btn-sm" onclick="App.openTechnicienModal(${t.id})" title="Modifier">✏️</button>
            <button class="btn btn-secondary btn-sm" onclick="App.handleDeleteTechnicien(${t.id})" title="Supprimer" style="color:var(--danger);">🗑️</button>
          </div>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  openTechnicienModal(id = null) {
    this.populateAgencesDropdowns();
    const modalTitle = document.getElementById("modal-technicien-title");
    const idInput = document.getElementById("form-technicien-id");
    const matInput = document.getElementById("form-technicien-matricule");
    const prenomInput = document.getElementById("form-technicien-prenom");
    const nomInput = document.getElementById("form-technicien-nom");
    const emailInput = document.getElementById("form-technicien-email");
    const telInput = document.getElementById("form-technicien-tel");
    const baseSelect = document.getElementById("form-technicien-base");
    const statutSelect = document.getElementById("form-technicien-statut");
    const heuresInput = document.getElementById("form-technicien-heures");
    const specsBox = document.getElementById("technicien-specialites-checkboxes");

    specsBox.innerHTML = "";
    let selectedSpecIds = [];

    const defaultAgency = (this.state.settings.agences && this.state.settings.agences[0]) || "Casablanca";

    if (id) {
      const t = this.state.techniciens.find(x => x.id === id);
      if (!t) return;
      modalTitle.textContent = "Modifier le Technicien";
      idInput.value = t.id;
      matInput.value = t.matricule;
      matInput.disabled = true;
      prenomInput.value = t.prenom;
      nomInput.value = t.nom;
      emailInput.value = t.email || "";
      telInput.value = t.telephone || "";
      baseSelect.value = t.base || defaultAgency;
      statutSelect.value = t.statut || "Actif";
      heuresInput.value = t.heuresHebdo || this.state.settings.defaultHours || 40;
      selectedSpecIds = (t.specialites || []).map(s => s.id);
    } else {
      modalTitle.textContent = "Nouveau Technicien";
      idInput.value = "";
      matInput.value = "";
      matInput.disabled = false;
      prenomInput.value = "";
      nomInput.value = "";
      emailInput.value = "";
      telInput.value = "";
      baseSelect.value = defaultAgency;
      statutSelect.value = "Actif";
      heuresInput.value = this.state.settings.defaultHours || 40;
    }

    this.state.specialites.forEach(s => {
      const isChecked = selectedSpecIds.includes(s.id);
      const label = document.createElement("label");
      label.style.display = "flex";
      label.style.alignItems = "center";
      label.style.gap = "6px";
      label.style.fontSize = "0.82rem";
      label.style.cursor = "pointer";
      label.innerHTML = `
        <input type="checkbox" name="tech-specialite" value="${s.id}" ${isChecked ? 'checked' : ''}>
        <span>${s.nom}</span>
      `;
      specsBox.appendChild(label);
    });

    this.openModal("modal-technicien");
  },

  async handleSaveTechnicien(e) {
    e.preventDefault();
    const id = document.getElementById("form-technicien-id").value;
    const matricule = document.getElementById("form-technicien-matricule").value;
    const prenom = document.getElementById("form-technicien-prenom").value;
    const nom = document.getElementById("form-technicien-nom").value;
    const email = document.getElementById("form-technicien-email").value;
    const telephone = document.getElementById("form-technicien-tel").value;
    const base = document.getElementById("form-technicien-base").value;
    const statut = document.getElementById("form-technicien-statut").value;
    const heuresHebdo = parseInt(document.getElementById("form-technicien-heures").value);

    const checkedBoxes = document.querySelectorAll("input[name='tech-specialite']:checked");
    const specialiteIds = Array.from(checkedBoxes).map(cb => parseInt(cb.value));

    const payload = {
      id: id ? parseInt(id) : 0,
      matricule,
      prenom,
      nom,
      email,
      telephone,
      base,
      statut,
      heuresHebdo,
      disponible: statut === "Actif",
      specialiteIds
    };

    try {
      let res;
      if (id) {
        res = await this.fetchApi(`/api/techniciens/${id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
      } else {
        res = await this.fetchApi("/api/techniciens", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
      }

      this.closeModal("modal-technicien");
      this.showToast(`Technicien ${prenom} ${nom} enregistré !`);
      this.loadAllData();
    } catch (err) {
      console.error(err);
      this.showToast(err.message || "Erreur lors de l'enregistrement du technicien.", "error");
    }
  },

  async handleDeleteTechnicien(id) {
    if (!confirm("Voulez-vous vraiment supprimer ce technicien ?")) return;
    try {
      await this.fetchApi(`/api/techniciens/${id}`, { method: "DELETE" });
      this.showToast("Technicien supprimé avec succès.");
      this.loadAllData();
    } catch (err) {
      console.error(err);
      this.showToast(err.message || "Erreur lors de la suppression du technicien.", "error");
    }
  },

  /* ------------------------------------------------------------------------
   * 7. PLANIFICATION INTELLIGENTE & MOTEUR DE SCORING
   * ------------------------------------------------------------------------ */
  openPlanifierVisiteModal(targetEquipementId = null) {
    const eqSelect = document.getElementById("form-visite-equipement");
    const dateInput = document.getElementById("form-visite-date");
    const typeSelect = document.getElementById("form-visite-type");
    const dureeSelect = document.getElementById("form-visite-duree");
    const boxAutre = document.getElementById("box-visite-autre");

    if (boxAutre) boxAutre.style.display = "none";
    if (typeSelect) typeSelect.value = "Préventive";
    if (dureeSelect) dureeSelect.value = this.state.settings.defaultVisiteDuration || 120;

    const defaultDate = new Date();
    defaultDate.setDate(defaultDate.getDate() + 1);
    defaultDate.setHours(9, 0, 0, 0);
    const tzOffset = defaultDate.getTimezoneOffset() * 60000;
    dateInput.value = (new Date(defaultDate.getTime() - tzOffset)).toISOString().slice(0, 16);

    eqSelect.innerHTML = `<option value="">— Sélectionner un équipement —</option>`;
    this.state.equipements.forEach(e => {
      const opt = document.createElement("option");
      opt.value = e.id;
      opt.textContent = `${e.serialNumber} — ${e.nom} (${e.categorie}) [${e.clientNom || ''} / ${e.siteNom || ''}]`;
      if (targetEquipementId && e.id === targetEquipementId) {
        opt.selected = true;
      }
      eqSelect.appendChild(opt);
    });

    this.state.selectedTechnicienIdForPlanning = null;
    this.openModal("modal-visite");

    if (targetEquipementId) {
      this.updateTechnicienRecommendations();
    } else {
      document.getElementById("technicien-podium-container").innerHTML = `
        <div style="padding:14px; text-align:center; color:var(--text-muted); background:var(--bg); border-radius:var(--r-md); border:1px solid var(--border-light); font-size:0.85rem;">
          Sélectionnez un équipement ci-dessus pour calculer l'adéquation des techniciens.
        </div>
      `;
      const techSelect = document.getElementById("form-visite-technicien");
      techSelect.innerHTML = `<option value="">— Sélectionner un équipement d'abord —</option>`;
      techSelect.disabled = true;
    }
  },

  async updateTechnicienRecommendations() {
    const eqId = parseInt(document.getElementById("form-visite-equipement").value);
    const datePrevue = document.getElementById("form-visite-date").value;
    const dureeMinutes = parseInt(document.getElementById("form-visite-duree").value) || 120;
    const podiumContainer = document.getElementById("technicien-podium-container");
    const techSelect = document.getElementById("form-visite-technicien");
    const statusIndicator = document.getElementById("scoring-status-indicator");

    if (!eqId) {
      podiumContainer.innerHTML = `
        <div style="padding:14px; text-align:center; color:var(--text-muted); background:var(--bg); border-radius:var(--r-md); border:1px solid var(--border-light); font-size:0.85rem;">
          Sélectionnez un équipement ci-dessus pour calculer l'adéquation des techniciens.
        </div>
      `;
      techSelect.innerHTML = `<option value="">— Sélectionner un équipement d'abord —</option>`;
      techSelect.disabled = true;
      return;
    }

    if (statusIndicator) statusIndicator.textContent = "Calcul du score en cours...";

    const params = new URLSearchParams({
      equipementId: eqId,
      datePrevue: new Date(datePrevue).toISOString(),
      dureeMinutes
    });

    const recommendations = await this.fetchApi(`/api/visites/recommandations-techniciens?${params.toString()}`);
    if (statusIndicator) statusIndicator.textContent = "Recommandation dynamique calculée";

    if (!recommendations || recommendations.length === 0) {
      podiumContainer.innerHTML = `
        <div style="padding:14px; text-align:center; color:var(--danger); background:rgba(224,90,90,0.08); border-radius:var(--r-md); border:1px solid var(--danger); font-size:0.85rem;">
          Aucun technicien disponible.
        </div>
      `;
      techSelect.innerHTML = `<option value="">Aucun technicien disponible</option>`;
      techSelect.disabled = true;
      return;
    }

    const top3 = recommendations.slice(0, 3);
    const medals = ["🥇", "🥈", "🥉"];

    if (!this.state.selectedTechnicienIdForPlanning || !recommendations.some(r => r.technicienId === this.state.selectedTechnicienIdForPlanning)) {
      this.state.selectedTechnicienIdForPlanning = top3[0].technicienId;
    }

    podiumContainer.innerHTML = "";
    top3.forEach((rec, idx) => {
      const isSelected = rec.technicienId === this.state.selectedTechnicienIdForPlanning;
      const card = document.createElement("div");
      card.className = `podium-card ${isSelected ? 'selected' : ''}`;
      card.onclick = () => this.selectTechnicienForPlanning(rec.technicienId);

      card.innerHTML = `
        <div style="display:flex; align-items:center;">
          <div class="podium-medal">${medals[idx]}</div>
          <div class="podium-info">
            <div class="podium-name">
              ${rec.nomComplet}
              <span class="badge badge-planifiee" style="font-size:0.68rem; padding:1px 5px;">${rec.matricule}</span>
            </div>
            <div class="podium-tags">
              <span class="podium-tag podium-tag-success">${rec.detailsCompetence}</span>
              <span class="podium-tag ${rec.scoreDisponibilite >= 25 ? 'podium-tag-success' : 'podium-tag-warning'}">${rec.detailsDisponibilite}</span>
              <span class="podium-tag">${rec.detailsCharge}</span>
              <span class="podium-tag">${rec.detailsProximite}</span>
            </div>
          </div>
        </div>
        <div class="podium-score-box">
          <span class="podium-score-val">${rec.score} / 100</span>
          <span class="podium-score-lbl">Score adéquation</span>
        </div>
      `;
      podiumContainer.appendChild(card);
    });

    techSelect.innerHTML = "";
    recommendations.forEach(r => {
      const opt = document.createElement("option");
      opt.value = r.technicienId;
      opt.selected = r.technicienId === this.state.selectedTechnicienIdForPlanning;
      opt.textContent = `${r.matricule} — ${r.nomComplet} (${r.score}/100 • ${r.detailsCompetence} • ${r.heuresRestantes}h disp)`;
      techSelect.appendChild(opt);
    });
    techSelect.disabled = false;

    techSelect.onchange = (e) => {
      this.selectTechnicienForPlanning(parseInt(e.target.value));
    };
  },

  selectTechnicienForPlanning(techId) {
    this.state.selectedTechnicienIdForPlanning = techId;
    const techSelect = document.getElementById("form-visite-technicien");
    if (techSelect) techSelect.value = techId;

    const cards = document.querySelectorAll(".podium-card");
    const top3 = (this.state.techniciens || []).slice(0, 3);
    cards.forEach((card, idx) => {
      const rec = top3[idx];
      card.classList.toggle("selected", rec && rec.id === techId);
    });
  },

  async handleCreateVisite(e) {
    e.preventDefault();
    const equipementId = parseInt(document.getElementById("form-visite-equipement").value);
    const typeVisite = document.getElementById("form-visite-type").value;
    const typeVisiteAutre = document.getElementById("form-visite-autre-libelle")?.value?.trim() || null;
    const description = document.getElementById("form-visite-autre-desc")?.value?.trim() || null;
    const techVal = document.getElementById("form-visite-technicien").value;
    const technicienId = techVal ? parseInt(techVal) : null;
    const datePrevue = document.getElementById("form-visite-date").value;
    const dureeEstimeeMinutes = parseInt(document.getElementById("form-visite-duree").value) || 120;

    if (typeVisite === "Autre" && !typeVisiteAutre) {
      this.showToast("Le champ 'Précisez le type de visite' est obligatoire pour le type Autre.", "error");
      return;
    }

    const payload = {
      equipementId,
      typeVisite,
      typeVisiteAutre,
      description,
      technicienId,
      datePrevue: new Date(datePrevue).toISOString(),
      dureeEstimeeMinutes,
      statut: "Planifiée"
    };

    try {
      const result = await this.fetchApi("/api/visites", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      this.closeModal("modal-visite");
      this.showToast(`Nouvelle visite ${result?.reference || ''} planifiée avec succès !`);
      this.loadAllData();
    } catch (err) {
      console.error(err);
      this.showToast(err.message || "Erreur lors de la planification de la visite.", "error");
    }
  },

  /* ------------------------------------------------------------------------
   * 8. RAPPORT DE VISITE & MODE TERRAIN
   * ------------------------------------------------------------------------ */
  openRapportModal(visiteId) {
    const visite = this.state.visites.find(v => v.id === visiteId);
    if (!visite) return;

    document.getElementById("form-rapport-id").value = visite.id;
    document.getElementById("form-rapport-statut").value = visite.statut === "En retard" ? "En retard" : "Validée";
    document.getElementById("form-rapport-duree-reelle").value = visite.dureeReelleMinutes || visite.dureeEstimeeMinutes || 120;
    document.getElementById("form-rapport-texte").value = visite.rapportTechnique || "";
    document.getElementById("form-rapport-actions").value = visite.actionsCorrectives || "";

    const btnExportPdf = document.getElementById("btn-export-pv");
    if (btnExportPdf) {
      btnExportPdf.style.display = visite.statut === "Validée" ? "inline-block" : "none";
    }

    this.openModal("modal-rapport");
  },

  async handleUpdateRapport(e) {
    e.preventDefault();
    const id = parseInt(document.getElementById("form-rapport-id").value);
    const statut = document.getElementById("form-rapport-statut").value;
    const dureeReelleMinutes = parseInt(document.getElementById("form-rapport-duree-reelle").value) || null;
    const rapportTechnique = document.getElementById("form-rapport-texte").value;
    const actionsCorrectives = document.getElementById("form-rapport-actions").value;

    const payload = { statut, dureeReelleMinutes, rapportTechnique, actionsCorrectives };

    try {
      await this.fetchApi(`/api/visites/${id}/statut`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      this.closeModal("modal-rapport");
      this.showToast("Fiche de visite enregistrée et validée !");
      this.loadAllData();
    } catch (err) {
      console.error(err);
      this.showToast(err.message || "Erreur lors de la validation du rapport.", "error");
    }
  },

  populateActiveTechViewSelect() {
    const sel = document.getElementById("select-active-tech-view");
    if (!sel) return;
    sel.innerHTML = `<option value="">— Tous les techniciens —</option>`;
    this.state.techniciens.forEach(t => {
      const opt = document.createElement("option");
      opt.value = t.id;
      opt.textContent = `${t.prenom} ${t.nom} (${t.matricule})`;
      sel.appendChild(opt);
    });
  },

  renderTechnicien() {
    const tbody = document.getElementById("table-technicien-body");
    if (!tbody) return;
    tbody.innerHTML = "";

    const filterTechId = parseInt(document.getElementById("select-active-tech-view")?.value) || null;
    let list = this.state.visites;
    if (filterTechId) {
      list = list.filter(v => v.technicienId === filterTechId);
    }

    if (list.length === 0) {
      tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: var(--text-muted); padding: 2rem;">Aucune intervention assignée.</td></tr>`;
      return;
    }

    list.forEach(v => {
      const tr = document.createElement("tr");
      const dateFormatted = new Date(v.datePrevue).toLocaleDateString("fr-FR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
      const typeDisplay = v.typeVisiteAffiche || (v.typeVisite === "Autre" && v.typeVisiteAutre ? `Autre (${v.typeVisiteAutre})` : v.typeVisite);

      tr.innerHTML = `
        <td><strong>${v.reference}</strong></td>
        <td><span class="badge badge-planifiee">${typeDisplay}</span></td>
        <td>${v.equipementNom}</td>
        <td>${v.clientNom} / ${v.siteNom}</td>
        <td>${dateFormatted}</td>
        <td><span class="badge ${this.getBadgeClass(v.statut)}">${v.statut}</span></td>
        <td>
          <button class="btn btn-primary btn-sm" onclick="App.openRapportModal(${v.id})">Saisir Rapport</button>
        </td>
      `;
      tbody.appendChild(tr);
    });
  },

  /* ------------------------------------------------------------------------
   * 9. MARCHÉS & CLIENTS
   * ------------------------------------------------------------------------ */
  renderClients() {
    const mBody = document.getElementById("table-marches-body");
    if (mBody) {
      mBody.innerHTML = "";
      if (this.state.marches.length === 0) {
        mBody.innerHTML = `<tr><td colspan="7" style="text-align:center; color:var(--text-muted); padding:2rem;">Aucun contrat de marché enregistré.</td></tr>`;
      } else {
        this.state.marches.forEach(m => {
          const tr = document.createElement("tr");
          const dDebut = m.dateDebut ? new Date(m.dateDebut).toLocaleDateString("fr-FR") : "—";
          const dFin = m.dateFin ? new Date(m.dateFin).toLocaleDateString("fr-FR") : "—";
          const prevues = m.visitesAnnuellesPrevues || 12;
          const realisees = m.visitesRealisees || 0;
          const avancement = prevues > 0 ? Math.min(100, Math.round((realisees / prevues) * 100)) : 0;

          tr.innerHTML = `
            <td><strong>${m.codeMarche}</strong></td>
            <td>${m.libelle}</td>
            <td><strong>${m.clientNom}</strong></td>
            <td>${dDebut} → ${dFin}</td>
            <td>${m.slaHeures}h</td>
            <td>
              <div style="font-size:0.75rem; display:flex; justify-content:space-between;">
                <span>${realisees}/${prevues}</span>
                <span>${avancement}%</span>
              </div>
              <div class="tech-hours-bar">
                <div class="tech-hours-fill" style="width:${avancement}%;"></div>
              </div>
            </td>
            <td><span class="badge ${m.statut === 'Actif' ? 'badge-validee' : 'badge-retard'}">${m.statut}</span></td>
          `;
          mBody.appendChild(tr);
        });
      }
    }

    const cBody = document.getElementById("table-clients-body");
    if (cBody) {
      cBody.innerHTML = "";
      if (this.state.clients.length === 0) {
        cBody.innerHTML = `<tr><td colspan="6" style="text-align:center; color:var(--text-muted); padding:2rem;">Aucun client enregistré.</td></tr>`;
      } else {
        this.state.clients.forEach(c => {
          const tr = document.createElement("tr");
          const sitesStr = (c.sites || []).map(s => `<span class="badge badge-planifiee" style="font-size:0.7rem;">${s.nomSite} (${s.ville})</span>`).join(" ") || "—";

          tr.innerHTML = `
            <td><strong>${c.codeClient}</strong></td>
            <td><strong>${c.nomSociete}</strong></td>
            <td>${c.contactPrincipal || "—"}</td>
            <td>${c.email || "—"}</td>
            <td>${c.telephone || "—"}</td>
            <td>${sitesStr}</td>
          `;
          cBody.appendChild(tr);
        });
      }
    }
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
        opt.textContent = `${c.nomSociete} — ${s.nomSite} (${s.ville})`;
        select.appendChild(opt);
      });
    });
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

    try {
      const result = await this.fetchApi("/api/marches", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
      });

      this.closeModal("modal-marche");
      this.showToast(`Nouveau marché ${result?.codeMarche || ''} créé !`);
      this.loadAllData();
    } catch (err) {
      console.error(err);
      this.showToast(err.message || "Erreur lors de la création du marché.", "error");
    }
  },

  /* ── IMPORT EXCEL MARCHÉS ── */
  openExcelImportModal() {
    const fileInput = document.getElementById("input-excel-file");
    if (fileInput) fileInput.value = "";
    document.getElementById("btn-preview-excel").disabled = true;
    this._excelImportAllRows = null;
    this.showImportStep(1);
    this.openModal("modal-import-excel");
  },

  showImportStep(step) {
    document.getElementById("import-step-1").style.display = step === 1 ? "block" : "none";
    document.getElementById("import-step-2").style.display = step === 2 ? "block" : "none";
  },

  async handleExcelPreview() {
    const fileInput = document.getElementById("input-excel-file");
    if (!fileInput || !fileInput.files.length) return;

    const btn = document.getElementById("btn-preview-excel");
    btn.textContent = "Analyse en cours…";
    btn.disabled = true;

    const formData = new FormData();
    formData.append("file", fileInput.files[0]);

    try {
      const resp = await fetch("/api/marches/import/preview", { method: "POST", body: formData });
      const result = await resp.json();
      this._excelImportAllRows = result.allRows;

      document.getElementById("import-summary").innerHTML = `<strong>${result.rowCount}</strong> marché(s) détecté(s).`;

      const tbody = document.getElementById("import-preview-body");
      tbody.innerHTML = "";
      (result.preview || []).forEach(r => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td>${r.rowIndex}</td>
          <td><strong>${r.reference || "—"}</strong></td>
          <td>${r.clientNom || "—"}</td>
          <td>${r.dateDebut || "—"}</td>
          <td>${r.dateFin || "—"}</td>
          <td>${r.typeContrat || "—"}</td>
          <td>${r.visitesAnnuellesPrevues ?? 0}</td>
          <td>${r.sites || "—"}</td>
          <td style="color: ${r.parseWarning ? '#f5a623' : '#34c38f'}; font-size:0.75rem;">${r.parseWarning || "✓"}</td>
        `;
        tbody.appendChild(tr);
      });

      this.showImportStep(2);
    } catch (e) {
      console.error(e);
    } finally {
      btn.textContent = "Analyser le fichier";
      btn.disabled = false;
    }
  },

  async handleExcelConfirm() {
    if (!this._excelImportAllRows || !this._excelImportAllRows.length) return;
    const btn = document.getElementById("btn-confirm-import");
    btn.textContent = "Import en cours…";
    btn.disabled = true;

    try {
      const resp = await fetch("/api/marches/import/confirm", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(this._excelImportAllRows)
      });
      const result = await resp.json();
      this.closeModal("modal-import-excel");
      this.showToast(`Import terminé : ${result.imported} marché(s) importé(s).`);
      this.loadAllData();
    } catch (e) {
      this.showToast("Erreur d'importation.", "error");
    } finally {
      btn.textContent = "Confirmer l'import";
      btn.disabled = false;
      this._excelImportAllRows = null;
    }
  },

  /* ------------------------------------------------------------------------
   * 10. CENTRE D'EXPORTATION (PDF / EXCEL / CSV)
   * ------------------------------------------------------------------------ */
  openExportModal(source) {
    const sourceEl = document.getElementById("export-source");
    if (sourceEl) sourceEl.value = source;
    this.openModal("modal-export-preview");
    this.refreshExportPreview();
    this.updateExportFormatDesc();
  },

  async refreshExportPreview() {
    const source = document.getElementById("export-source").value;
    const thead = document.getElementById("export-preview-thead");
    const tbody = document.getElementById("export-preview-tbody");
    const info = document.getElementById("export-preview-info");

    thead.innerHTML = "";
    tbody.innerHTML = "";

    if (source === "visites") {
      info.textContent = `Aperçu des visites (${this.state.visites.length} enregistrements)`;
      thead.innerHTML = `<tr><th>Référence</th><th>Type</th><th>Équipement</th><th>Client/Site</th><th>Technicien</th><th>Date</th><th>Statut</th></tr>`;
      this.state.visites.slice(0, 5).forEach(v => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td><strong>${v.reference}</strong></td>
          <td>${v.typeVisite}</td>
          <td>${v.equipementNom}</td>
          <td>${v.clientNom} / ${v.siteNom}</td>
          <td>${v.technicienNom}</td>
          <td>${new Date(v.datePrevue).toLocaleDateString("fr-FR")}</td>
          <td>${v.statut}</td>
        `;
        tbody.appendChild(tr);
      });
    } else {
      info.textContent = `Aperçu des marchés (${this.state.marches.length} enregistrements)`;
      thead.innerHTML = `<tr><th>Code</th><th>Libellé</th><th>Client</th><th>Période</th><th>SLA</th><th>Statut</th></tr>`;
      this.state.marches.slice(0, 5).forEach(m => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
          <td><strong>${m.codeMarche}</strong></td>
          <td>${m.libelle}</td>
          <td>${m.clientNom}</td>
          <td>${new Date(m.dateDebut).toLocaleDateString("fr-FR")} → ${new Date(m.dateFin).toLocaleDateString("fr-FR")}</td>
          <td>${m.slaHeures}h</td>
          <td>${m.statut}</td>
        `;
        tbody.appendChild(tr);
      });
    }
  },

  updateExportFormatDesc() {
    const fmt = document.getElementById("export-format").value;
    const desc = document.getElementById("export-format-desc");
    const map = {
      excel: "📊 Fichier tableur Excel officiel (.xlsx) avec styles et en-têtes formatés.",
      pdf: "📄 Document PDF au format A4 paysage haute résolution généré par QuestPDF.",
      csv: "📋 Fichier texte CSV encodé en UTF-8 standard avec séparateur point-virgule."
    };
    desc.textContent = map[fmt] || "";
  },

  doExport() {
    const source = document.getElementById("export-source").value;
    const format = document.getElementById("export-format").value;
    const url = source === "visites" ? `/api/visites/export?format=${format}` : `/api/marches/export?format=${format}`;
    const ext = format === "excel" ? "xlsx" : (format === "pdf" ? "pdf" : "csv");
    const link = document.createElement("a");
    link.href = url;
    link.download = `Export_${source}_${new Date().toISOString().slice(0,10)}.${ext}`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    this.closeModal("modal-export-preview");
  },

  /* ------------------------------------------------------------------------
   * 11. HELPERS & MODALES
   * ------------------------------------------------------------------------ */
  openModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.add("active");
  },

  closeModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) modal.classList.remove("active");
  },

  getBadgeClass(statut) {
    switch (statut) {
      case "Validée": return "badge-validee";
      case "En retard": return "badge-retard";
      case "En cours": return "badge-revision";
      default: return "badge-planifiee";
    }
  },

  showToast(message, type = "success") {
    const container = document.getElementById("toast-container");
    if (!container) return;

    const toast = document.createElement("div");
    toast.className = `toast ${type === 'error' ? 'toast-error' : ''}`;
    if (type === 'error') {
      toast.style.background = "#e05a5a";
      toast.style.color = "#ffffff";
    }
    toast.textContent = message;

    container.appendChild(toast);
    setTimeout(() => {
      toast.style.opacity = "0";
      setTimeout(() => toast.remove(), 300);
    }, 3800);
  }
};
