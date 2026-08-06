using System.Diagnostics.Metrics;

namespace AfriWallet.Merchant.Api.Production;

public sealed class MerchantTelemetry
{
    private readonly Counter<long> _merchantCounter;
    private readonly Counter<long> _qrCounter;
    private readonly Counter<long> _posCounter;
    private readonly Counter<long> _settlementCounter;
    private readonly Counter<long> _dashboardCounter;
    private readonly Counter<long> _kycCounter;

    public MerchantTelemetry()
    {
        var meter = new Meter("AfriWallet.Merchant");
        _merchantCounter = meter.CreateCounter<long>("afw_merchants_total");
        _qrCounter = meter.CreateCounter<long>("afw_qr_payments_total");
        _posCounter = meter.CreateCounter<long>("afw_pos_transactions_total");
        _settlementCounter = meter.CreateCounter<long>("afw_settlements_total");
        _dashboardCounter = meter.CreateCounter<long>("afw_dashboard_requests_total");
        _kycCounter = meter.CreateCounter<long>("afw_merchant_kyc_total");
    }

    public void TrackMerchantCreated() => _merchantCounter.Add(1);
    public void TrackQrCreated() => _qrCounter.Add(1);
    public void TrackPosTransaction() => _posCounter.Add(1);
    public void TrackSettlement() => _settlementCounter.Add(1);
    public void TrackDashboardRequest() => _dashboardCounter.Add(1);
    public void TrackKyc() => _kycCounter.Add(1);
}
