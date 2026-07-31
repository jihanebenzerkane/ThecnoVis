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
    totalVisites: 24,
    visitesPlanifiees: 12,
    visitesEnRetard: 3,
    visitesValidees: 9,
    totalEquipements: 18,
    equipementsCritiques: 4,
    tauxConformite: 87.5,
    alertesUrgent: [
      {
        id: 102,
        reference: "VIS-2026-1002",
        typeVisite: "Curative",
        equipement: "Groupe Électrogène Caterpillar 1500kVA",
        site: "Datacenter Tit Mellil",
        datePrevue: "2026-07-28T09:00:00",
        statut: "En retard",
        scorePriorite: 92.0
      },
      {
        id: 105,
        reference: "VIS-2026-1004",
        typeVisite: "Préventive",
        equipement: "Armoire TGBT Principal Masterpact",
        site: "Siège Social Casablanca",
        datePrevue: "2026-08-01T14:30:00",
        statut: "Planifiée",
        scorePriorite: 88.5
      }
    ]
  },

  visites: [
    {
      id: 1,
      reference: "VIS-2026-1001",
      typeVisite: "Préventive",
      equipementId: 1,
      equipementNom: "Groupe Froid Trane Centravac",
      equipementSerial: "EQ-HVAC-901",
      siteNom: "Siège Social Casablanca",
      clientNom: "TotalEnergies Maroc",
      technicienAssigne: "Amine El Amrani",
      datePrevue: "2026-08-02T10:00:00",
      dateRealisee: null,
      dureeEstimeeMinutes: 120,
      statut: "Planifiée",
      scorePriorite: 65.5,
      rapportTechnique: "",
      actionsCorrectives: ""
    },
    {
      id: 2,
      reference: "VIS-2026-1002",
      typeVisite: "Curative",
      equipementId: 2,
      equipementNom: "Groupe Électrogène Caterpillar 1500kVA",
      equipementSerial: "EQ-GE-404",
      siteNom: "Datacenter Tit Mellil",
      clientNom: "Attijariwafa Data Center",
      technicienAssigne: "Hassan Chraibi",
      datePrevue: "2026-07-28T09:00:00",
      dateRealisee: null,
      dureeEstimeeMinutes: 180,
      statut: "En retard",
      scorePriorite: 92.0,
      rapportTechnique: "Alerte pression huile moteur au démarrage.",
      actionsCorrectives: "Remplacement filtre huile et purge système."
    },
    {
      id: 3,
      reference: "VIS-2026-1003",
      typeVisite: "Audit",
      equipementId: 3,
      equipementNom: "Transformateur Schneider Triphasé 20kV",
      equipementSerial: "EQ-TRF-208",
      siteNom: "Complexe Chimique Safi",
      clientNom: "OCP Group Safi",
      technicienAssigne: "Nadia Berrada",
      datePrevue: "2026-07-26T11:00:00",
      dateRealisee: "2026-07-26T12:30:00",
      dureeEstimeeMinutes: 90,
      statut: "Validée",
      scorePriorite: 45.0,
      rapportTechnique: "Analyse diélectrique huile conforme. Isolation optimale.",
      actionsCorrectives: "Rien à signaler."
    },
    {
      id: 4,
      reference: "VIS-2026-1004",
      typeVisite: "Préventive",
      equipementId: 5,
      equipementNom: "Armoire TGBT Principal Masterpact",
      equipementSerial: "EQ-TGBT-101",
      siteNom: "Siège Social Casablanca",
      clientNom: "TotalEnergies Maroc",
      technicienAssigne: "Amine El Amrani",
      datePrevue: "2026-08-01T14:30:00",
      dateRealisee: null,
      dureeEstimeeMinutes: 150,
      statut: "Planifiée",
      scorePriorite: 88.5,
      rapportTechnique: "",
      actionsCorrectives: ""
    }
  ],

  equipements: [
    {
      id: 1,
      serialNumber: "EQ-HVAC-901",
      nom: "Groupe Froid Trane Centravac",
      categorie: "HVAC",
      siteNom: "Siège Social Casablanca",
      clientNom: "TotalEnergies Maroc",
      dateInstallation: "2020-04-12",
      criticiticite: 5,
      scoreSante: 78,
      scoreRisque: 38,
      statut: "Opérationnel",
      derniereVisite: "2026-07-03",
      prochaineVisitePrevue: "2026-08-02"
    },
    {
      id: 2,
      serialNumber: "EQ-GE-404",
      nom: "Groupe Électrogène Caterpillar 1500kVA",
      categorie: "Groupe Électrogène",
      siteNom: "Datacenter Tit Mellil",
      clientNom: "Attijariwafa Data Center",
      dateInstallation: "2019-11-05",
      criticiticite: 5,
      scoreSante: 62,
      scoreRisque: 74,
      statut: "Maintenance Requise",
      derniereVisite: "2026-06-15",
      prochaineVisitePrevue: "2026-07-28"
    },
    {
      id: 3,
      serialNumber: "EQ-TRF-208",
      nom: "Transformateur Schneider Triphasé 20kV",
      categorie: "Transformateur",
      siteNom: "Complexe Chimique Safi",
      clientNom: "OCP Group Safi",
      dateInstallation: "2018-06-20",
      criticiticite: 4,
      scoreSante: 91,
      scoreRisque: 18,
      statut: "Opérationnel",
      derniereVisite: "2026-07-16",
      prochaineVisitePrevue: "2026-08-15"
    },
    {
      id: 4,
      serialNumber: "EQ-CMP-302",
      nom: "Compresseur Atlas Copco GA75",
      categorie: "Compresseur",
      siteNom: "Complexe Chimique Safi",
      clientNom: "OCP Group Safi",
      dateInstallation: "2021-08-30",
      criticiticite: 3,
      scoreSante: 85,
      scoreRisque: 22,
      statut: "Opérationnel",
      derniereVisite: "2026-07-21",
      prochaineVisitePrevue: "2026-08-20"
    },
    {
      id: 5,
      serialNumber: "EQ-TGBT-101",
      nom: "Armoire TGBT Principal Masterpact",
      categorie: "TGBT",
      siteNom: "Siège Social Casablanca",
      clientNom: "TotalEnergies Maroc",
      dateInstallation: "2017-02-14",
      criticiticite: 5,
      scoreSante: 55,
      scoreRisque: 82,
      statut: "En Révision",
      derniereVisite: "2026-06-01",
      prochaineVisitePrevue: "2026-08-01"
    }
  ],

  clients: [
    {
      id: 1,
      codeClient: "CL-001",
      nomSociete: "TotalEnergies Maroc",
      contactPrincipal: "Karim Benali",
      email: "k.benali@totalenergies.ma",
      telephone: "+212 522 10 20 30",
      adresse: "Bd Zerktouni, Casablanca",
      sites: [{ nomSite: "Siège Social Casablanca", ville: "Casablanca" }]
    },
    {
      id: 2,
      codeClient: "CL-002",
      nomSociete: "OCP Group Safi",
      contactPrincipal: "Sarah Mansouri",
      email: "s.mansouri@ocpgroup.ma",
      telephone: "+212 524 88 99 00",
      adresse: "Zone Industrielle, Safi",
      sites: [{ nomSite: "Complexe Chimique Safi", ville: "Safi" }]
    },
    {
      id: 3,
      codeClient: "CL-003",
      nomSociete: "Attijariwafa Data Center",
      contactPrincipal: "Youssef Tazi",
      email: "y.tazi@attijariwafa.com",
      telephone: "+212 522 45 67 89",
      adresse: "Sidi Maârouf, Casablanca",
      sites: [{ nomSite: "Datacenter Tit Mellil", ville: "Casablanca" }]
    }
  ],

  marches: [
    {
      id: 1,
      codeMarche: "MAR-2026-089",
      libelle: "Maintenance Préventive HVAC & Groupes Électrogènes",
      client: { nomSociete: "TotalEnergies Maroc" },
      dateDebut: "2026-01-01",
      dateFin: "2026-12-31",
      slaHeures: 12,
      visitesAnnuellesPrevues: 24,
      visitesRealisees: 14,
      statut: "Actif"
    },
    {
      id: 2,
      codeMarche: "MAR-2026-112",
      libelle: "Maintenance Haute Tension & Transformateurs",
      client: { nomSociete: "OCP Group Safi" },
      dateDebut: "2025-06-01",
      dateFin: "2027-05-31",
      slaHeures: 4,
      visitesAnnuellesPrevues: 48,
      visitesRealisees: 32,
      statut: "Actif"
    },
    {
      id: 3,
      codeMarche: "MAR-2026-045",
      libelle: "Audit & Maintenance Datacenter",
      client: { nomSociete: "Attijariwafa Data Center" },
      dateDebut: "2026-03-15",
      dateFin: "2027-03-14",
      slaHeures: 2,
      visitesAnnuellesPrevues: 52,
      visitesRealisees: 20,
      statut: "Actif"
    }
  ]
};
