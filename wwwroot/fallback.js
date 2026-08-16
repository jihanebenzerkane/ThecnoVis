/**
 * TechnoVIS - Isolated Offline Fallback Module
 * ============================================
 * Ce module contient des données de secours utilisées UNIQUEMENT si l'API backend REST
 * (/api/visites, /api/equipements, etc.) est temporairement injoignable.
 * Il permet d'assurer la résilience de l'interface graphique.
 */

window.TechnoVisFallback = {
  isOfflineMode: false,

  stats: {
    totalVisites: 0,
    visitesPlanifiees: 0,
    visitesEnRetard: 0,
    visitesValidees: 0,
    totalEquipements: 0,
    equipementsCritiques: 0,
    tauxConformite: 100,
    alertesUrgent: []
  },

  visites: [],
  equipements: [],
  clients: [],
  marches: []
};
