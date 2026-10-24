using SimulatoreFinanziamenti.Api.Services;

namespace SimulatoreFinanziamenti.Tests;

public class CalcoloFinanziarioTests
{
    [Fact]
    public void RataCostante_MutuoClassico()
    {
        // 200.000 € al 3% per 20 anni: rata di riferimento 1.109,20 €.
        Assert.Equal(1109.20m, CalcoloFinanziario.RataCostante(200_000, 3m, 240));
    }

    [Fact]
    public void RataCostante_TassoZero()
    {
        Assert.Equal(500m, CalcoloFinanziario.RataCostante(12_000, 0m, 24));
    }

    [Fact]
    public void Piano_RimborsaTuttoIlCapitale()
    {
        var piano = CalcoloFinanziario.PianoAmmortamento(150_000, 3.4m, 300, new DateOnly(2026, 1, 15), _ => 0);

        Assert.Equal(300, piano.Count);
        Assert.Equal(150_000m, piano.Sum(r => r.QuotaCapitale));
        Assert.Equal(0m, piano[^1].DebitoResiduo);
        Assert.Equal(new DateOnly(2026, 2, 15), piano[0].DataScadenza);
        Assert.All(piano, r => Assert.Equal(r.Rata, r.QuotaCapitale + r.QuotaInteressi));
        // L'ultima rata assorbe gli arrotondamenti della rata al centesimo accumulati sulla durata.
        Assert.InRange(Math.Abs(piano[^1].Rata - piano[0].Rata), 0m, 5m);
    }

    [Fact]
    public void Taeg_SenzaCostiCoincideConTassoEffettivo()
    {
        var piano = CalcoloFinanziario.PianoAmmortamento(10_000, 6m, 60, new DateOnly(2026, 1, 1), _ => 0);
        var taeg = CalcoloFinanziario.Taeg(10_000, piano.Select(r => r.Rata).ToList());

        // (1 + 6%/12)^12 - 1 = 6,17%
        Assert.Equal(6.17m, taeg);
    }

    [Fact]
    public void Taeg_CrescoConICosti()
    {
        var piano = CalcoloFinanziario.PianoAmmortamento(10_000, 6m, 60, new DateOnly(2026, 1, 1), _ => 0);
        var taeg = CalcoloFinanziario.Taeg(10_000 - 300, piano.Select(r => r.Rata + 2).ToList());

        Assert.True(taeg > 7.5m && taeg < 8.5m, $"TAEG inatteso: {taeg}");
    }
}
