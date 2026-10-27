using System.Security.Cryptography;
using SimulatoreFinanziamenti.Api.Dtos;

namespace SimulatoreFinanziamenti.Api.Services;

/// <summary>
/// Punto di integrazione con il core banking della banca. Per collegare il sistema reale basta
/// registrare un'implementazione che invii la <see cref="PraticaFinanziamento"/> al suo servizio.
/// </summary>
public interface ICoreBanking
{
    Task<RispostaCoreBanking> InviaPraticaAsync(PraticaFinanziamento pratica, CancellationToken annullamento);
}

/// <summary>Core banking simulato: accetta ogni pratica e assegna un numero di pratica fittizio.</summary>
public class CoreBankingSimulato(TimeProvider orologio) : ICoreBanking
{
    public Task<RispostaCoreBanking> InviaPraticaAsync(PraticaFinanziamento pratica, CancellationToken annullamento)
    {
        var adesso = orologio.GetLocalNow();
        var numero = $"PF-{adesso:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1_000_000):D6}";
        return Task.FromResult(new RispostaCoreBanking(numero, "Ricevuta", adesso));
    }
}
