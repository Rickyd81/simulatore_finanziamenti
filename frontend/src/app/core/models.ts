export type TipologiaProdotto = 'Mutuo' | 'PrestitoPersonale' | 'PrestitoFinalizzato';
export type Finalita =
  | 'AcquistoPrimaCasa'
  | 'AcquistoSecondaCasa'
  | 'Surroga'
  | 'Ristrutturazione'
  | 'Liquidita'
  | 'Auto'
  | 'Arredamento'
  | 'Studi'
  | 'Altro';
export type TipoTasso = 'Fisso' | 'Variabile';
export type CategoriaCondizione =
  | 'Tasso'
  | 'SpesaIstruttoria'
  | 'SpesaPerizia'
  | 'CommissioneIncassoRata'
  | 'Assicurazione'
  | 'ImpostaSostitutiva'
  | 'ImpostaBollo'
  | 'AltraSpesa';
export type TipoValore = 'Percentuale' | 'Importo';
export type Periodicita = 'UnaTantum' | 'Mensile' | 'Annuale';

/** Finalità per cui il cliente chiede un mutuo (serve il valore dell'immobile). */
export const FINALITA_MUTUO: Finalita[] = ['AcquistoPrimaCasa', 'AcquistoSecondaCasa', 'Surroga'];

export interface Voce {
  codice: string;
  descrizione: string;
}

export interface Tabelle {
  tipologie: Voce[];
  finalita: Voce[];
  tipiTasso: Voce[];
  categorieCondizione: Voce[];
  tipiValore: Voce[];
  periodicita: Voce[];
  province: Voce[];
}

export interface Prodotto {
  id: number;
  codice: string;
  nome: string;
  descrizione: string | null;
  tipologia: TipologiaProdotto;
  tipoTasso: TipoTasso;
  indiceRiferimento: string | null;
  finalitaAmmesse: Finalita[];
  importoMinimo: number;
  importoMassimo: number;
  durataMinimaMesi: number;
  durataMassimaMesi: number;
  etaMinima: number;
  etaMassimaRichiedente: number | null;
  etaMassimaFineFinanziamento: number | null;
  ltvMassimo: number | null;
  provinceAmmesse: string[];
  attivo: boolean;
  dataInizioValidita: string;
  dataFineValidita: string | null;
  condizioni: Condizione[];
}

export interface Condizione {
  id: number;
  prodottoId: number;
  codice: string;
  categoria: CategoriaCondizione;
  descrizione: string;
  tipoValore: TipoValore;
  valore: number;
  importoMinimo: number | null;
  importoMassimo: number | null;
  periodicita: Periodicita;
  inclusaNelTaeg: boolean;
  fasciaImportoDa: number | null;
  fasciaImportoA: number | null;
  fasciaDurataDaMesi: number | null;
  fasciaDurataAMesi: number | null;
  fasciaLtvDa: number | null;
  fasciaLtvA: number | null;
  finalita: Finalita | null;
  dataInizioValidita: string;
  dataFineValidita: string | null;
}

export interface IndiceRiferimento {
  codice: string;
  descrizione: string;
  valore: number;
  dataRilevazione: string;
}

export interface RichiestaSimulazione {
  finalita: Finalita;
  importo: number;
  durataMesi: number;
  valoreImmobile: number | null;
  provincia: string | null;
  etaRichiedente: number;
}

export interface VoceCosto {
  codice: string;
  categoria: CategoriaCondizione;
  descrizione: string;
  periodicita: Periodicita;
  importo: number;
  importoTotale: number;
  inclusaNelTaeg: boolean;
}

export interface RataPiano {
  numero: number;
  dataScadenza: string;
  rata: number;
  quotaCapitale: number;
  quotaInteressi: number;
  spese: number;
  debitoResiduo: number;
}

export interface Offerta {
  prodottoId: number;
  codice: string;
  nome: string;
  descrizione: string | null;
  tipologia: TipologiaProdotto;
  tipoTasso: TipoTasso;
  indiceRiferimento: string | null;
  valoreIndice: number | null;
  spread: number | null;
  tan: number;
  taeg: number;
  rata: number;
  importo: number;
  durataMesi: number;
  totaleInteressi: number;
  totaleSpese: number;
  importoTotaleDovuto: number;
  costi: VoceCosto[];
  pianoAmmortamento: RataPiano[];
}

export interface ProdottoNonDisponibile {
  prodottoId: number;
  nome: string;
  motivi: string[];
}

export interface RisultatoSimulazione {
  ltv: number | null;
  offerte: Offerta[];
  nonDisponibili: ProdottoNonDisponibile[];
}
