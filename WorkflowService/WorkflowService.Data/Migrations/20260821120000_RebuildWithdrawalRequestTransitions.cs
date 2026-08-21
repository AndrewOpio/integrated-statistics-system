using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RebuildWithdrawalRequestTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @EntityTypeId INT = (SELECT Id FROM EntityTypes WHERE Name = 'Withdrawal Request');
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @By NVARCHAR(100) = 'wtibingana';

-- Clear every existing WithdrawalRequest transition (cascades TransitionAttachmentRequirements).
DELETE FROM WorkflowTransitions WHERE EntityTypeId = @EntityTypeId;

-- Drop dead/unintended statuses: Cancel is not a concept in this domain (Recall is the only
-- withdrawal mechanism), and VerifyExceptional/Pending Exceptional Verification was a mistake,
-- not intended design.
DELETE FROM StatusTypes
WHERE EntityTypeId = @EntityTypeId
  AND Name IN ('Cancelled', 'Cancel Complete', 'Pending Exceptional Verification');

-- Rename statuses for clarity (see CONTEXT.md ""Renamed statuses"").
UPDATE StatusTypes SET Name = 'Pending RTGS Code', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending RTGS Verification';
UPDATE StatusTypes SET Name = 'Pending RTGS Confirmation', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Withdrawal Confirmation';
UPDATE StatusTypes SET Name = 'RTGS Confirmation Failed', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'Verification Failed';
UPDATE StatusTypes SET Name = 'Exceptional Approval Rejected', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'Reject Exceptional Approval';

-- RejectRecall (Currency Approver) needs a real ToStatus row to match against even though the
-- code-side logic (ADR-0001) overrides it with a direct status mutation back to whatever status
-- the recall was requested from. Create the placeholder status if this environment doesn't
-- already have it.
IF NOT EXISTS (SELECT 1 FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Rejected')
BEGIN
    INSERT INTO StatusTypes (Name, EntityTypeId, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy)
    VALUES ('Recall Rejected', @EntityTypeId, @Now, @Now, @By, @By);
END

DECLARE @Draft INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Internal Draft');
DECLARE @PendingInternalApproval INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Internal Approval');
DECLARE @ReturnedForInternalRevision INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Returned for Internal Revision');
DECLARE @PendingRtgsCode INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending RTGS Code');
DECLARE @PendingRtgsConfirmation INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending RTGS Confirmation');
DECLARE @PendingExceptionalApproval INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Exceptional Approval');
DECLARE @PendingVerification INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Verification');
DECLARE @PendingApproval INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Approval');
DECLARE @RecallRequested INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Requested');
DECLARE @RecallVerified INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Verified');
DECLARE @RecallApproved INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Approved');
DECLARE @RecallComplete INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Complete');
DECLARE @RecallRejected INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Rejected');
DECLARE @RtgsConfirmationFailed INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'RTGS Confirmation Failed');
DECLARE @ExceptionalApprovalRejected INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Exceptional Approval Rejected');
DECLARE @RejectExceptionalApprovalComplete INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Reject Exceptional Approval Complete');
DECLARE @Approved INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Approved');
DECLARE @Rejected INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Rejected');
DECLARE @RejectComplete INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Reject Complete');

-- RequiredRole ints are WorkflowService's copy of IdentityService's ApplicationRoles.Id — no FK
-- across services, so these are only valid where the roles were seeded in this same order.
DECLARE @CbMaker INT = 22;
DECLARE @CbApprover INT = 23;
DECLARE @BackOffice INT = 32;
DECLARE @PanelA INT = 25;
DECLARE @PanelB INT = 26;
DECLARE @CurrencyApprover INT = 4;

INSERT INTO WorkflowTransitions
    (EntityTypeId, FromStatusId, ToStatusId, ActionName, RequiredRole, RequiresComments, DisplayLabel, ButtonClass,
     IsActive, IsDeleted, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, NotificationEmails, NotificationRoleIds,
     SendNotification, RequiresAttachments)
VALUES
    -- CB Maker / CB Approver
    (@EntityTypeId, @Draft, @PendingInternalApproval, 'SubmitForInternalApproval', @CbMaker, 1, 'Submit for Approval', 'btn-success', 1, 0, @Now, @Now, @By, @By, 'wtibingana@bou.or.ug', NULL, 1, 0),
    (@EntityTypeId, @PendingInternalApproval, @ReturnedForInternalRevision, 'ReturnForRevision', @CbApprover, 1, 'Return for Revision', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @ReturnedForInternalRevision, @PendingInternalApproval, 'ResubmitForApproval', @CbMaker, 1, 'Resubmit', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingInternalApproval, @PendingRtgsCode, 'ApproveToRTGSCode', @CbApprover, 0, 'Approve (Pending RTGS Code)', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingInternalApproval, @PendingRtgsConfirmation, 'ExceptionalWithdrawalRequest', @CbApprover, 1, 'Approve Exceptional Request', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),

    -- RequestRecall: CB Maker or CB Approver, from every status Recall can branch off from.
    (@EntityTypeId, @PendingRtgsCode, @RecallRequested, 'RequestRecall', @CbMaker, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingRtgsCode, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingRtgsConfirmation, @RecallRequested, 'RequestRecall', @CbMaker, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingRtgsConfirmation, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalApproval, @RecallRequested, 'RequestRecall', @CbMaker, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalApproval, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingVerification, @RecallRequested, 'RequestRecall', @CbMaker, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingVerification, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingApproval, @RecallRequested, 'RequestRecall', @CbMaker, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingApproval, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),

    -- Back Office
    (@EntityTypeId, @PendingRtgsConfirmation, @PendingVerification, 'Submit_Verification', @BackOffice, 1, 'RTGS Confirmed To Verification', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingRtgsConfirmation, @PendingExceptionalApproval, 'Submit_Verification_Exceptional', @BackOffice, 1, 'RTGS Confirmed To Exceptional Approval', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingRtgsConfirmation, @RtgsConfirmationFailed, 'RTGS Confirmation Failed', @BackOffice, 1, 'RTGS Confirmation Failed', 'btn-danger', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @RtgsConfirmationFailed, @ReturnedForInternalRevision, 'ReturnForRevision', @BackOffice, 1, 'Return for Revision', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalApproval, @PendingVerification, 'ApproveExceptional', @BackOffice, 1, 'Approve Exceptional Request', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalApproval, @ExceptionalApprovalRejected, 'RejectExceptional', @BackOffice, 1, 'Reject Exceptional Request', 'btn-danger', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @ExceptionalApprovalRejected, @RejectExceptionalApprovalComplete, 'Complete Reject Exceptional', @BackOffice, 0, 'Complete Reject Exceptional', 'btn-danger', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @RecallRequested, @RecallVerified, 'VerifyRecall', @BackOffice, 1, 'Verify Recall', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @RecallApproved, @RecallComplete, 'Complete Recall', @BackOffice, 0, 'Complete Recall', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @Rejected, @RejectComplete, 'Complete Reject', @BackOffice, 0, 'Complete Reject', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),

    -- Panel B (ABM)
    (@EntityTypeId, @PendingVerification, @ReturnedForInternalRevision, 'ReturnForRevision', @PanelB, 1, 'Return for Revision', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingVerification, @PendingApproval, 'Submit for Approval', @PanelB, 1, 'Submit for Approval', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),

    -- Panel A (BM)
    (@EntityTypeId, @PendingApproval, @Approved, 'Approve', @PanelA, 1, 'Approve', 'btn-success', 1, 0, @Now, @Now, @By, @By, 'wtibingana@bou.or.ug', NULL, 1, 1),
    (@EntityTypeId, @PendingApproval, @Rejected, 'Reject', @PanelA, 1, 'Reject', 'btn-danger', 1, 0, @Now, @Now, @By, @By, 'wtibingana@bou.or.ug', NULL, 1, 0),

    -- Currency Approver
    (@EntityTypeId, @RecallVerified, @RecallApproved, 'ApproveRecall', @CurrencyApprover, 1, 'Approve Recall', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @RecallVerified, @RecallRejected, 'RejectRecall', @CurrencyApprover, 1, 'Reject Recall', 'btn-danger', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0);

-- Re-attach the Approve step's attachment requirement (cascaded away when its old row was cleared).
DECLARE @ApproveTransitionId INT = (
    SELECT Id FROM WorkflowTransitions
    WHERE EntityTypeId = @EntityTypeId AND ActionName = 'Approve' AND FromStatusId = @PendingApproval AND ToStatusId = @Approved
);
INSERT INTO TransitionAttachmentRequirements
    (WorkflowTransitionId, AttachmentName, AllowedFormats, DisplayOrder, IsRequired, IsDeleted, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy)
VALUES
    (@ApproveTransitionId, 'Withdrawal Request Slip', 'pdf', 1, 1, 0, @Now, @Now, @By, @By);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @EntityTypeId INT = (SELECT Id FROM EntityTypes WHERE Name = 'Withdrawal Request');
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @By NVARCHAR(100) = 'admin';

DELETE FROM WorkflowTransitions WHERE EntityTypeId = @EntityTypeId;

DELETE FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Rejected';

UPDATE StatusTypes SET Name = 'Pending RTGS Verification', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending RTGS Code';
UPDATE StatusTypes SET Name = 'Pending Withdrawal Confirmation', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending RTGS Confirmation';
UPDATE StatusTypes SET Name = 'Verification Failed', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'RTGS Confirmation Failed';
UPDATE StatusTypes SET Name = 'Reject Exceptional Approval', ModifiedDate = @Now, ModifiedBy = @By
    WHERE EntityTypeId = @EntityTypeId AND Name = 'Exceptional Approval Rejected';

IF NOT EXISTS (SELECT 1 FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Cancelled')
    INSERT INTO StatusTypes (Name, EntityTypeId, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy) VALUES ('Cancelled', @EntityTypeId, @Now, @Now, @By, @By);
IF NOT EXISTS (SELECT 1 FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Cancel Complete')
    INSERT INTO StatusTypes (Name, EntityTypeId, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy) VALUES ('Cancel Complete', @EntityTypeId, @Now, @Now, @By, @By);
IF NOT EXISTS (SELECT 1 FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Exceptional Verification')
    INSERT INTO StatusTypes (Name, EntityTypeId, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy) VALUES ('Pending Exceptional Verification', @EntityTypeId, @Now, @Now, @By, @By);

DECLARE @Draft INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Internal Draft');
DECLARE @PendingInternalApproval INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Internal Approval');
DECLARE @ReturnedForInternalRevision INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Returned for Internal Revision');
DECLARE @PendingRtgsVerification INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending RTGS Verification');
DECLARE @PendingWithdrawalConfirmation INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Withdrawal Confirmation');
DECLARE @PendingExceptionalApproval INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Exceptional Approval');
DECLARE @PendingExceptionalVerification INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Exceptional Verification');
DECLARE @PendingVerification INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Verification');
DECLARE @PendingApproval INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Pending Approval');
DECLARE @RecallRequested INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Requested');
DECLARE @RecallVerified INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Verified');
DECLARE @RecallApproved INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Approved');
DECLARE @RecallComplete INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Recall Complete');
DECLARE @VerificationFailed INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Verification Failed');
DECLARE @RejectExceptionalApproval INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Reject Exceptional Approval');
DECLARE @RejectExceptionalApprovalComplete INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Reject Exceptional Approval Complete');
DECLARE @Approved INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Approved');
DECLARE @Rejected INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Rejected');
DECLARE @RejectComplete INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Reject Complete');
DECLARE @Cancelled INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Cancelled');
DECLARE @CancelComplete INT = (SELECT Id FROM StatusTypes WHERE EntityTypeId = @EntityTypeId AND Name = 'Cancel Complete');

DECLARE @CbMaker INT = 22;
DECLARE @CbApprover INT = 23;
DECLARE @BackOffice INT = 32;
DECLARE @PanelA INT = 25;
DECLARE @PanelB INT = 26;
DECLARE @CurrencyApprover INT = 4;

INSERT INTO WorkflowTransitions
    (EntityTypeId, FromStatusId, ToStatusId, ActionName, RequiredRole, RequiresComments, DisplayLabel, ButtonClass,
     IsActive, IsDeleted, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy, NotificationEmails, NotificationRoleIds,
     SendNotification, RequiresAttachments)
VALUES
    (@EntityTypeId, @Draft, @PendingInternalApproval, 'SubmitForInternalApproval', @CbMaker, 1, 'Submit for Approval', 'btn-success', 1, 0, @Now, @Now, @By, @By, 'wtibingana@bou.or.ug', NULL, 1, 0),
    (@EntityTypeId, @PendingInternalApproval, @ReturnedForInternalRevision, 'ReturnForRevision', @CbApprover, 1, 'Return for Revision', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @ReturnedForInternalRevision, @PendingInternalApproval, 'ResubmitForApproval', @CbMaker, 1, 'Resubmit', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingInternalApproval, @PendingRtgsVerification, 'ApproveToRTGSVefification', @CbApprover, 0, 'Approve (Pending RTGS Verification)', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingWithdrawalConfirmation, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalApproval, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingVerification, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingApproval, @RecallRequested, 'RequestRecall', @CbApprover, 1, 'Request Recall', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingWithdrawalConfirmation, @PendingExceptionalApproval, 'Submit_Verification_Exceptional', @BackOffice, 1, 'RTGS Confirmed To Exceptional Approval', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingWithdrawalConfirmation, @PendingVerification, 'Submit_Verification', @BackOffice, 1, 'RTGS Confirmed To Verification', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingWithdrawalConfirmation, @VerificationFailed, 'RTGS Confirmation Failed', @BackOffice, 1, 'RTGS Confirmation Failed', 'btn-danger', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingInternalApproval, @PendingWithdrawalConfirmation, 'ExceptionalWithdrawaRequest', @CbApprover, 1, 'Approve Exceptional Request', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingVerification, @PendingApproval, 'Submit for Approval', @PanelB, 1, 'SubmitForApproval', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingVerification, @ReturnedForInternalRevision, 'ReturnForRevision', @PanelB, 1, 'Return for Revision', 'btn-warning', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingApproval, @Approved, 'Approve', @PanelA, 1, 'Approve', 'btn-success', 1, 0, @Now, @Now, @By, @By, 'wtibingana@bou.or.ug', NULL, 1, 1),
    (@EntityTypeId, @PendingApproval, @Rejected, 'Reject', @PanelA, 1, 'Reject', 'btn-danger', 1, 0, @Now, @Now, @By, @By, 'wtibingana@bou.or.ug', NULL, 1, 0),
    (@EntityTypeId, @RejectExceptionalApproval, @RejectExceptionalApprovalComplete, 'Complete Reject Exceptional', @BackOffice, 0, 'Complete Reject Exceptional', 'btn-danger', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @Rejected, @RejectComplete, 'Complete Reject', @BackOffice, 0, 'Complete Reject', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @Cancelled, @CancelComplete, 'Complete Cancel', @BackOffice, 0, 'Complete Cancel', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @RecallApproved, @RecallComplete, 'Complete Recall', @BackOffice, 0, 'Complete Recall', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalApproval, @PendingExceptionalVerification, 'VerifyExceptional', @BackOffice, 1, 'Verify Exceptional', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalVerification, @PendingVerification, 'ApproveExceptional', @CurrencyApprover, 1, 'Approve Exceptional Request', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @PendingExceptionalVerification, @RejectExceptionalApproval, 'RejectExceptional', @CurrencyApprover, 1, 'Reject Exceptional Request', 'btn-danger', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @RecallRequested, @RecallVerified, 'VerifyRecall', @BackOffice, 1, 'Verify Recall', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0),
    (@EntityTypeId, @RecallVerified, @RecallApproved, 'ApproveRecall', @CurrencyApprover, 1, 'Approve Recall', 'btn-success', 1, 0, @Now, @Now, @By, @By, NULL, NULL, 0, 0);

DECLARE @ApproveTransitionId INT = (
    SELECT Id FROM WorkflowTransitions
    WHERE EntityTypeId = @EntityTypeId AND ActionName = 'Approve' AND FromStatusId = @PendingApproval AND ToStatusId = @Approved
);
INSERT INTO TransitionAttachmentRequirements
    (WorkflowTransitionId, AttachmentName, AllowedFormats, DisplayOrder, IsRequired, IsDeleted, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy)
VALUES
    (@ApproveTransitionId, 'Withdrawal Request Slip', 'pdf', 1, 1, 0, @Now, @Now, @By, @By);
");
        }
    }
}
