namespace ERechnung.Core.Models;

/// <summary>
/// Unveränderliche Steuersummen für einen einzelnen Steuersatz.
/// </summary>
public sealed record RechnungsSteuergruppe(
    decimal Steuersatz,
    decimal Nettobetrag,
    decimal Steuerbetrag);
