import { formattaDigitato, formattaImporto, leggiImporto } from './importo';

describe('importo', () => {
  it('formatta con i punti delle migliaia e la virgola decimale', () => {
    expect(formattaImporto(150000)).toBe('150.000');
    expect(formattaImporto(1250.5)).toBe('1.250,5');
    expect(formattaImporto(null)).toBe('');
  });

  it('legge il testo formattato come numero', () => {
    expect(leggiImporto('150.000')).toBe(150000);
    expect(leggiImporto('1.250,50')).toBe(1250.5);
    expect(leggiImporto('')).toBeNull();
  });

  it('riformatta mentre si digita', () => {
    expect(formattaDigitato('1500000')).toBe('1.500.000');
    expect(formattaDigitato('1.50000')).toBe('150.000');
    expect(formattaDigitato('1250,')).toBe('1.250,');
    expect(formattaDigitato('1250,567')).toBe('1.250,56');
    expect(formattaDigitato('00120a')).toBe('120');
  });
});
