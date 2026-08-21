-- Verifies that a WorkflowService database's `Withdrawal Request` WorkflowTransitions match
-- workflow-transitions.md's ✅/🔧 rows exactly (ActionName, From, To, RequiredRole), per ticket
-- 01's acceptance criteria. Run against any environment after applying migration
-- 20260821120000_RebuildWithdrawalRequestTransitions.
--
-- Usage: sqlcmd -S <server> -d <WorkflowService database> -C -i verify-workflow-transitions.sql

DECLARE @EntityTypeId INT = (SELECT Id FROM EntityTypes WHERE Name = 'Withdrawal Request');

IF OBJECT_ID('tempdb..#Expected') IS NOT NULL DROP TABLE #Expected;
CREATE TABLE #Expected (FromStatus NVARCHAR(200), ToStatus NVARCHAR(200), ActionName NVARCHAR(200), RequiredRole INT);

INSERT INTO #Expected (FromStatus, ToStatus, ActionName, RequiredRole) VALUES
    -- CB Maker / CB Approver
    ('Internal Draft', 'Pending Internal Approval', 'SubmitForInternalApproval', 22),
    ('Pending Internal Approval', 'Returned for Internal Revision', 'ReturnForRevision', 23),
    ('Returned for Internal Revision', 'Pending Internal Approval', 'ResubmitForApproval', 22),
    ('Pending Internal Approval', 'Pending RTGS Code', 'ApproveToRTGSCode', 23),
    ('Pending Internal Approval', 'Pending RTGS Confirmation', 'ExceptionalWithdrawalRequest', 23),
    ('Pending RTGS Code', 'Recall Requested', 'RequestRecall', 22),
    ('Pending RTGS Code', 'Recall Requested', 'RequestRecall', 23),
    ('Pending RTGS Confirmation', 'Recall Requested', 'RequestRecall', 22),
    ('Pending RTGS Confirmation', 'Recall Requested', 'RequestRecall', 23),
    ('Pending Exceptional Approval', 'Recall Requested', 'RequestRecall', 22),
    ('Pending Exceptional Approval', 'Recall Requested', 'RequestRecall', 23),
    ('Pending Verification', 'Recall Requested', 'RequestRecall', 22),
    ('Pending Verification', 'Recall Requested', 'RequestRecall', 23),
    ('Pending Approval', 'Recall Requested', 'RequestRecall', 22),
    ('Pending Approval', 'Recall Requested', 'RequestRecall', 23),

    -- Back Office
    ('Pending RTGS Confirmation', 'Pending Verification', 'Submit_Verification', 32),
    ('Pending RTGS Confirmation', 'Pending Exceptional Approval', 'Submit_Verification_Exceptional', 32),
    ('Pending RTGS Confirmation', 'RTGS Confirmation Failed', 'RTGS Confirmation Failed', 32),
    ('RTGS Confirmation Failed', 'Returned for Internal Revision', 'ReturnForRevision', 32),
    ('Pending Exceptional Approval', 'Pending Verification', 'ApproveExceptional', 32),
    ('Pending Exceptional Approval', 'Exceptional Approval Rejected', 'RejectExceptional', 32),
    ('Exceptional Approval Rejected', 'Reject Exceptional Approval Complete', 'Complete Reject Exceptional', 32),
    ('Recall Requested', 'Recall Verified', 'VerifyRecall', 32),
    ('Recall Approved', 'Recall Complete', 'Complete Recall', 32),
    ('Rejected', 'Reject Complete', 'Complete Reject', 32),

    -- Panel B (ABM)
    ('Pending Verification', 'Returned for Internal Revision', 'ReturnForRevision', 26),
    ('Pending Verification', 'Pending Approval', 'Submit for Approval', 26),

    -- Panel A (BM)
    ('Pending Approval', 'Approved', 'Approve', 25),
    ('Pending Approval', 'Rejected', 'Reject', 25),

    -- Currency Approver
    ('Recall Verified', 'Recall Approved', 'ApproveRecall', 4),
    ('Recall Verified', 'Recall Rejected', 'RejectRecall', 4);

IF OBJECT_ID('tempdb..#Actual') IS NOT NULL DROP TABLE #Actual;
SELECT fs.Name AS FromStatus, ts.Name AS ToStatus, wt.ActionName, wt.RequiredRole
INTO #Actual
FROM WorkflowTransitions wt
JOIN StatusTypes fs ON wt.FromStatusId = fs.Id
JOIN StatusTypes ts ON wt.ToStatusId = ts.Id
WHERE wt.EntityTypeId = @EntityTypeId AND wt.IsDeleted = 0;

PRINT '--- Rows in DB but NOT expected (should be empty) ---';
SELECT * FROM #Actual
EXCEPT
SELECT * FROM #Expected;

PRINT '--- Rows expected but MISSING from DB (should be empty) ---';
SELECT * FROM #Expected
EXCEPT
SELECT * FROM #Actual;

PRINT '--- Statuses this ticket removed that must not be referenced (should be empty) ---';
SELECT wt.Id, fs.Name AS FromStatus, ts.Name AS ToStatus, wt.ActionName
FROM WorkflowTransitions wt
JOIN StatusTypes fs ON wt.FromStatusId = fs.Id
JOIN StatusTypes ts ON wt.ToStatusId = ts.Id
WHERE wt.EntityTypeId = @EntityTypeId
  AND (fs.Name IN ('Cancelled', 'Cancel Complete', 'Pending Exceptional Verification',
                    'Pending RTGS Verification', 'Pending Withdrawal Confirmation', 'Verification Failed', 'Reject Exceptional Approval')
    OR ts.Name IN ('Cancelled', 'Cancel Complete', 'Pending Exceptional Verification',
                    'Pending RTGS Verification', 'Pending Withdrawal Confirmation', 'Verification Failed', 'Reject Exceptional Approval'));

DROP TABLE #Expected;
DROP TABLE #Actual;
