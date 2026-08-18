# Merchant Settlement Routing Policy

Required upstream decision: `CaptureEligible`, `Approved`; Merchant Registry `Active`; Merchant Verification `Verified`.

Routes: `MerchantSettlement`, `MerchantPayout`. Route selection is an orchestration target only; it does not prove external financial rails are active.
