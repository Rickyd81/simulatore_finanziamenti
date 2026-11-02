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
  /** Codice del pricing richiamato, dello stesso tipo di tasso; obbligatorio per il variabile. */
  codicePricing: string | null;
  /** Spread importato dal pricing (copia modificabile); null se il prodotto non richiama un pricing. */
  spread: number | null;
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

/** Dati economici comuni alle condizioni dei prodotti e a quelle dell'anagrafica condizioni. */
export interface DatiCondizione {
  id: number;
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

export interface Condizione extends DatiCondizione {
  prodottoId: number;
  /** Codice della condizione dell'anagrafica da cui è stata importata, null se inserita a mano. */
  codiceAnagrafica: string | null;
}

/** Condizione pre-confezionata dell'anagrafica condizioni. */
export interface CondizioneAnagrafica extends DatiCondizione {
  gruppo: string | null;
}

/** Pricing dell'anagrafica pricing: tasso di riferimento e spread, per tipo di tasso. */
export interface Pricing {
  codice: string;
  descrizione: string;
  /** Tipo di tasso dei prodotti che lo possono richiamare; null solo per i dati delle versioni precedenti. */
  tipoTasso: TipoTasso | null;
  /** Tasso di riferimento in percentuale: indice (es. Euribor) per il variabile, tasso base (es. IRS) per il fisso. */
  valore: number;
  dataRilevazione: string;
  /** Spread in percentuale; null solo per i pricing creati prima dell'introduzione dello spread. */
  spread: number | null;
}

export interface RichiestaSimulazione {
  finalita: Finalita;
  importo: number;
  durataMesi: number;
  valoreImmobile: number | null;
  provincia: string | null;
  etaRichiedente: number;
}

export interface DatiCliente {
  nome: string;
  cognome: string;
  codiceFiscale: string;
  email: string;
  telefono: string | null;
  consensoPrivacy: boolean;
}

export interface RichiestaFinanziamento {
  prodottoId: number;
  simulazione: RichiestaSimulazione;
  cliente: DatiCliente;
}

export interface EsitoRichiestaFinanziamento {
  numeroPratica: string;
  stato: string;
  dataRicezione: string;
  messaggio: string;
  /** Tracciato standard inviato al core banking (schema PraticaFinanziamento in /openapi/v1.json). */
  pratica: Record<string, unknown>;
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
