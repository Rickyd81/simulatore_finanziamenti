import { Offerta, RichiestaSimulazione, Tabelle } from '../core/models';

/** Dati necessari per stampare il preventivo dell'offerta selezionata. */
export interface DatiPreventivo {
  offerta: Offerta;
  simulazione: RichiestaSimulazione;
  ltv: number | null;
  tabelle: Tabelle | undefined;
}

const ARDESIA: [number, number, number] = [63, 77, 88];
const ROSSO: [number, number, number] = [198, 40, 40];
const TESTO: [number, number, number] = [31, 42, 48];
const TENUE: [number, number, number] = [92, 107, 116];

/** Separatore delle migliaia anche sotto i 10.000 (l'it-IT di default scrive 2350,00). */
const formato = (opzioni: Intl.NumberFormatOptions) =>
  new Intl.NumberFormat('it-IT', {
    ...opzioni,
    useGrouping: 'always',
  } as unknown as Intl.NumberFormatOptions);
const formatoEuro = formato({ style: 'currency', currency: 'EUR' });
const formatoNumero = formato({ minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatoAnni = formato({ maximumFractionDigits: 1 });
const euro = (v: number) => formatoEuro.format(v);
const numero = (v: number) => formatoNumero.format(v);
const percentuale = (v: number) => `${numero(v)} %`;
const data = (iso: string | Date) =>
  new Date(iso).toLocaleDateString('it-IT', { day: '2-digit', month: '2-digit', year: 'numeric' });

/**
 * Genera e scarica il PDF del preventivo: dati della simulazione, condizioni dell'offerta,
 * spese e piano di ammortamento. Le librerie PDF sono caricate solo al primo utilizzo.
 */
export async function scaricaPreventivoPdf(dati: DatiPreventivo): Promise<void> {
  const [{ jsPDF }, { autoTable }] = await Promise.all([
    import('jspdf'),
    import('jspdf-autotable'),
  ]);
  const { offerta: o, simulazione: s, ltv, tabelle } = dati;
  const decodifica = (tabella: keyof Tabelle, codice: string | null) =>
    (codice && tabelle?.[tabella].find((v) => v.codice === codice)?.descrizione) || codice || '';

  const doc = new jsPDF({ unit: 'mm', format: 'a4' });
  const larghezza = doc.internal.pageSize.getWidth();
  const margine = 15;
  const oggi = new Date();
  /** Posizione verticale dopo l'ultima tabella disegnata. */
  const dopoTabella = () =>
    (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY;

  // Intestazione
  doc.setFillColor(...ARDESIA);
  doc.rect(0, 0, larghezza, 24, 'F');
  doc.setFillColor(...ROSSO);
  doc.rect(0, 24, larghezza, 1.2, 'F');
  doc.setTextColor(255, 255, 255);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(16);
  doc.text('Preventivo di finanziamento', margine, 13);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(9);
  doc.text('Simulatore finanziamenti', margine, 19);
  doc.text(`Data: ${data(oggi)}`, larghezza - margine, 13, { align: 'right' });

  // Prodotto
  let y = 36;
  doc.setTextColor(...TESTO);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(14);
  doc.text(o.nome, margine, y);
  doc.setFont('helvetica', 'normal');
  doc.setFontSize(9);
  doc.setTextColor(...TENUE);
  y += 5;
  doc.text(`Codice prodotto ${o.codice}`, margine, y);
  if (o.descrizione) {
    const righe = doc.splitTextToSize(o.descrizione, larghezza - 2 * margine);
    y += 5;
    doc.text(righe, margine, y);
    y += (righe.length - 1) * 4;
  }

  // Riquadri con le cifre principali
  y += 6;
  const cifre: [string, string][] = [
    ['Rata mensile', euro(o.rata)],
    ['TAN', percentuale(o.tan)],
    ['TAEG', percentuale(o.taeg)],
    ['Totale dovuto', euro(o.importoTotaleDovuto)],
  ];
  const spazio = 4;
  const largRiquadro = (larghezza - 2 * margine - spazio * (cifre.length - 1)) / cifre.length;
  cifre.forEach(([etichetta, valore], i) => {
    const x = margine + i * (largRiquadro + spazio);
    doc.setDrawColor(201, 209, 214);
    doc.setLineWidth(0.3);
    doc.rect(x, y, largRiquadro, 17);
    doc.setFillColor(...ROSSO);
    doc.rect(x, y, 0.9, 17, 'F');
    doc.setFontSize(8);
    doc.setTextColor(...TENUE);
    doc.text(etichetta.toUpperCase(), x + 4, y + 6);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(...TESTO);
    doc.text(valore, x + 4, y + 13);
    doc.setFont('helvetica', 'normal');
  });
  y += 25;

  const stileSezione = {
    theme: 'plain' as const,
    styles: { fontSize: 9, cellPadding: 1.6, textColor: TESTO },
    columnStyles: { 0: { textColor: TENUE, cellWidth: 55 } },
    margin: { left: margine, right: margine },
  };
  const titolo = (testo: string, yTitolo: number) => {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(11);
    doc.setTextColor(...ARDESIA);
    doc.text(testo, margine, yTitolo);
    doc.setDrawColor(...ROSSO);
    doc.setLineWidth(0.4);
    doc.line(margine, yTitolo + 1.5, larghezza - margine, yTitolo + 1.5);
    doc.setFont('helvetica', 'normal');
    return yTitolo + 3;
  };

  // Dati della richiesta
  const richiesta: [string, string][] = [
    ['Finalità', decodifica('finalita', s.finalita)],
    ['Importo richiesto', euro(s.importo)],
  ];
  if (s.valoreImmobile !== null) richiesta.push(["Valore dell'immobile", euro(s.valoreImmobile)]);
  if (ltv !== null) richiesta.push(['Loan to value', percentuale(ltv)]);
  richiesta.push([
    'Durata',
    `${s.durataMesi} mesi (${formatoAnni.format(s.durataMesi / 12)} anni)`,
  ]);
  if (s.provincia) {
    richiesta.push([
      s.valoreImmobile !== null ? "Provincia dell'immobile" : 'Provincia di residenza',
      `${decodifica('province', s.provincia)} (${s.provincia})`,
    ]);
  }
  richiesta.push(['Età del richiedente', `${s.etaRichiedente} anni`]);
  autoTable(doc, { ...stileSezione, startY: titolo('Dati della richiesta', y), body: richiesta });

  // Condizioni economiche
  let tasso = decodifica('tipiTasso', o.tipoTasso);
  if (o.tipoTasso === 'Variabile' && o.valoreIndice !== null && o.spread !== null) {
    tasso += ` (${o.indiceRiferimento} ${numero(o.valoreIndice)}% + spread ${numero(o.spread)}%)`;
  }
  autoTable(doc, {
    ...stileSezione,
    startY: titolo('Condizioni economiche', dopoTabella() + 8),
    body: [
      ['Importo finanziato', euro(o.importo)],
      ['Durata', `${o.durataMesi} mesi`],
      ['Tipo di tasso', tasso],
      ['TAN', percentuale(o.tan)],
      ['TAEG', percentuale(o.taeg)],
      ['Rata mensile', euro(o.rata)],
      ['Totale interessi', euro(o.totaleInteressi)],
      ['Totale spese e imposte', euro(o.totaleSpese)],
      ['Importo totale dovuto', euro(o.importoTotaleDovuto)],
    ],
    didParseCell: (d) => {
      if (d.section === 'body' && d.row.index === 8) d.cell.styles.fontStyle = 'bold';
    },
  });

  const stileTabella = {
    theme: 'striped' as const,
    styles: { fontSize: 8.5, cellPadding: 1.6, textColor: TESTO },
    headStyles: { fillColor: ARDESIA, textColor: 255, fontStyle: 'bold' as const },
    alternateRowStyles: { fillColor: [244, 246, 248] as [number, number, number] },
    margin: { left: margine, right: margine, top: 20, bottom: 18 },
  };

  // Spese, commissioni e imposte
  autoTable(doc, {
    ...stileTabella,
    startY: titolo('Spese, commissioni e imposte', dopoTabella() + 8),
    head: [['Voce', 'Periodicità', 'Importo', 'Totale sulla durata', 'Nel TAEG']],
    body: o.costi.length
      ? o.costi.map((c) => [
          c.descrizione,
          decodifica('periodicita', c.periodicita),
          euro(c.importo),
          euro(c.importoTotale),
          c.inclusaNelTaeg ? 'Sì' : 'No',
        ])
      : [[{ content: 'Nessuna spesa prevista', colSpan: 5 }]],
    columnStyles: { 2: { halign: 'right' }, 3: { halign: 'right' } },
    didParseCell: (d) => {
      if (d.section === 'head' && (d.column.index === 2 || d.column.index === 3))
        d.cell.styles.halign = 'right';
    },
  });

  // Avvertenza
  y = dopoTabella() + 7;
  const avvertenza = doc.splitTextToSize(
    "Simulazione a scopo informativo, non costituisce offerta. Per i prodotti a tasso variabile il calcolo ipotizza che l'indice resti invariato per tutta la durata. Le condizioni definitive sono riportate nei documenti di trasparenza della banca.",
    larghezza - 2 * margine,
  );
  if (y + avvertenza.length * 4 > doc.internal.pageSize.getHeight() - 18) {
    doc.addPage();
    y = 20;
  }
  doc.setFontSize(8.5);
  doc.setTextColor(...TENUE);
  doc.text(avvertenza, margine, y);

  // Piano di ammortamento su pagina dedicata
  doc.addPage();
  autoTable(doc, {
    ...stileTabella,
    startY: titolo('Piano di ammortamento', 20),
    head: [
      ['N.', 'Scadenza', 'Rata', 'Quota capitale', 'Quota interessi', 'Spese', 'Debito residuo'],
    ],
    body: o.pianoAmmortamento.map((r) => [
      r.numero,
      data(r.dataScadenza),
      numero(r.rata),
      numero(r.quotaCapitale),
      numero(r.quotaInteressi),
      numero(r.spese),
      numero(r.debitoResiduo),
    ]),
    styles: { ...stileTabella.styles, fontSize: 8, cellPadding: 1.2, halign: 'right' },
    columnStyles: { 1: { halign: 'left' } },
    headStyles: { ...stileTabella.headStyles, halign: 'right' },
  });

  // Piè di pagina con numerazione
  const pagine = doc.getNumberOfPages();
  const altezza = doc.internal.pageSize.getHeight();
  for (let p = 1; p <= pagine; p++) {
    doc.setPage(p);
    doc.setFontSize(8);
    doc.setTextColor(...TENUE);
    doc.text(`${o.nome} – preventivo del ${data(oggi)}`, margine, altezza - 8);
    doc.text(`Pagina ${p} di ${pagine}`, larghezza - margine, altezza - 8, { align: 'right' });
  }

  doc.save(`preventivo-${o.codice}-${oggi.toISOString().slice(0, 10)}.pdf`);
}
