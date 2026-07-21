

namespace HRM.Components.Shared
{
    internal class SharedConfig
    {


        #region Attendance Log States

        public static class AttendanceLogStates
        {
            public const int Valid = 1;
            public const int Invalid = 2;
            public const int Edited = 3;
        }

        #endregion Attendance Log States


        #region "Request States"

        //*******************************************************************************

        public static class RequestStates
        {
            public const int INCOMPLETE = 1;
            public const int PENDING_VERIFICATION = 2;
            public const int PENDING_APPROVAL = 3;
            public const int APPROVED = 4;
            public const int CANCELLED = 5;
            public const int REJECTED = 6;
            public const int PENDING_PAYMENT = 7;
            public const int PAID = 8;

            public const int PENDING_SUPERVISOR_VERIFICATION = 9;
            public const int PENDING_STAFF_COMPLETION = 10;
            public const int PENDING_SUPERVISOR_APPROVAL = 11;
            public const int PENDING_HOD_APPROVAL = 12;
            public const int PENDING_HR_VERIFICATION = 13;
            public const int PENDING_HR_APPROVAL = 14;
            public const int PENDING_HR_HEAD_APPROVAL = 15;
            public const int PENDING_APPROVAL_PERSON_1 = 16;
            public const int PENDING_APPROVAL_PERSON_2 = 17;

            public const int ASSIGNED = 18;
            public const int COMPLETED = 19;
            public const int IN_PROGRESS = 20;
            public const int PENDING_REVIEW = 21;
            public const int ON_HOLD = 22;
            public const int UNASSIGNED = 23;
            public const int CLOSE_CANCELLED = 24;
            public const int CLOSE_COMPLETED = 25;
        }

        //*******************************************************************************

        #endregion "Request States"


        #region Operation Log Action Types

        //*******************************************************************************

        public static class OperationLogActionTypes
        {
            public const int DATA_COLLECTION_CREATE = 1;
            public const int DATA_COLLECTION_UPDATE = 2;
            public const int ROLES_CREATE = 3;
            public const int ROLES_UPDATE = 4;
            public const int USERS_CREATE = 5;
            public const int USERS_UPDATE = 6;
            public const int USERS_STATE_CHANGE = 50;
            public const int USERS_RESET_PASSWORD = 51;
            public const int USERS_FORGOT_PASSWORD = 52;
            public const int USERS_CHANGE_PASSWORD = 53;
            public const int FACILITY_REGISTRATION_CREATE = 7;
            public const int FACILITY_REGISTRATION_UPDATE = 8;
            public const int CRUISE_PERMITS_CREATE = 9;
            public const int CRUISE_PERMITS_UPDATE = 10;
            public const int USER_REGISTRATION_CREATE = 11;
            public const int USER_REGISTRATION_UPDATE = 12;
            public const int BUSINESS_ENTITY_CREATE = 13;
            public const int BUSINESS_ENTITY_UPDATE = 14;
            public const int REQUESTS_CREATE = 15;
            public const int REQUESTS_UPDATE = 16;
            public const int USERS_GROUP_CREATE = 17;
            public const int USERS_GROUP_UPDATE = 18;
            public const int USER_ORGANISATION_ROLE_CREATE = 19;
            public const int USER_ORGANISATION_ROLE_UPDATE = 20;
            public const int USER_GROUP_ROLE_CREATE = 21;
            public const int USER_GROUP_ROLE_UPDATE = 22;
            public const int USER_ROLE_CREATE = 23;
            public const int USER_ROLE_UPDATE = 24;
            public const int CHARTER_LICENSE_CREATE = 25;
            public const int CHARTER_LICENSE_UDPATE = 26;
            public const int DOCUMENT_UPLOAD = 27;
            public const int BUSINESS_ENTITY_DOCUMENT = 28;
            public const int SERVICE_DOCUMENT = 29;
            public const int REQUEST_DOCUMENT = 30;
            public const int DNR_LOOKUP_CACHE = 31;
            public const int BUSINESS_ENTITY_RELATION_CREATE = 32;
            public const int LOCATION_CREATE = 33;
            public const int BUSINESS_ENTITY_CONTACT = 34;
            public const int ONLINE_LOCATION_CREATE = 35;
            public const int ONLINE_LOCATION_UPDATE = 36;
            public const int IDCARD_CREATE = 37;
            public const int IDCARD_UPDATE = 38;
            public const int PASSPORT_CREATE = 39;
            public const int PASSPORT_UPDATE = 40;
            public const int BUSINESS_ENTITY_LINKED_SERVICE_CREATE = 41;
            public const int BUSINESS_ENTITY_LINKED_SERVICE_UPDATE = 47;
            public const int REQUEST_RELATED_DOCUMENT_CREATE = 42;
            public const int REQUEST_RELATED_DOCUMENT_DELETE = 43;
            public const int SERVICE_RELATED_DOCUMENT_CREATE = 44;
            public const int SERVICE_RELATED_DOCUMENT_DELETE = 45;
            public const int BUSINESS_ENTITY_RELATION_UPDATE = 46;
            public const int DATA_COLLECTION_STATE_CHANGE = 48;
            public const int REQUEST_STATE_CHANGE = 49;
            public const int USER_ORAGNISATION_ATTACHED = 56;
            public const int USER_ORGANISATION_DETACH = 55;
            public const int USER_ORGANISATION_CHANGE = 54;
            public const int TOUR_OPERATOR_CREATE = 57;
            public const int TOUR_OPERATOR_UPDATE = 58;
            public const int TOUR_OPERATOR_SERVICE_CREATE = 59;
            public const int TOUR_OPERATOR_SERVICE_UPDATE = 60;
            public const int TOUR_OPERATOR_SERVICE_STATE_CHANGE = 61;
            public const int TOUR_OPERATOR_ONLINE_CREATE = 62;
            public const int TOUR_OPERATOR_ONLINE_SERVICE_CREATE = 63;
            public const int TOUR_OPERATOR_ONLINE_SERVICE_STATE_CHANGE = 64;
            public const int FACILITY_REGISTRATION_ADDRESS_UPDATE = 65;
            public const int BUSINESS_ENTITY_ADDRESS_UPDATE = 66;
            public const int LOCATION_MODIFY = 69;
            public const int USER_SERVICE_ROLE_CREATE = 70;
            public const int USER_SERVICE_ROLE_EDIT = 71;
            public const int LOCAL_AGENT_CREATE = 72;
            public const int REQUEST_DOCUMENT_VERIFICATION_CREATE = 73;
            public const int REQUEST_DOCUMENT_VERIFICATION_EDIT = 74;
            public const int DESTINATION_DOCUMENT_VERIFICATION_CREATE = 75;
            public const int DESTINATION_DOCUMENT_VERIFICATION_EDIT = 76;
            public const int CHECKLIST_ITEM_CREATE = 77;
            public const int CHECKLIST_ITEM_EDIT = 78;
            public const int CRUISE_PERMIT_FEE_PAYMENT_CREATE = 79;
            public const int CRUISE_PERMIT_FEE_PAYMENT_ITEM_CREATE = 80;
            public const int CHARTER_LICENSE_FEE_PAYMENT_CREATE = 84;
            public const int CHARTER_LICENSE_FEE_PAYMENT_ITEM_CREATE = 85;
            public const int PAYMENT_CREATE = 81;
            public const int PAYMENT_EDIT = 82;
            public const int PAYMENT_ITEM_CREATE = 83;
            public const int PAYMENT_ITEM_EDIT = 84;
            public const int PAYMENT_ATTEMPT_RELATED_METHOD_ATTRIBUTE_VALUE_CREATE = 86;
            public const int PAYMENT_ATTEMPT_CREATE = 88;
            public const int PAYMENT_ATTEMPT_EDIT = 89;
            public const int ORGANISATION_STRUCTURE_CREATE = 90;
            public const int ORGANISATION_STRUCTURE_EDIT = 91;
            public const int KEY_HOLDER_CREATE = 92;
            public const int KEY_HOLDER_UPDATE = 93;
            public const int ORGANISASTION_STRUCTURE_HEAD_INCHARGE_CREATE = 94;
            public const int ORGANISASTION_STRUCTURE_HEAD_INCHARGE_UPDATE = 95;
            public const int BUSINESSENTITY_CALENDAR_CREATE = 96;
            public const int BUSINESSENTITY_CALENDAR_EDIT = 97;
            public const int CALENDAR_CREATE = 98;
            public const int CALENDAR_EDIT = 99;
            public const int CALENDAR_TYPE_CREATE = 100;
            public const int CALENDAR_TYPE_UPDATE = 101;
            public const int ORGANISATION_STRUCTURE_CALENDAR_CREATE = 104;
            public const int ORGANISATION_STRUCTURE_CALENDAR_EDIT = 105;
            public const int MONTH_CREATE = 102;
            public const int MONTH_UPDATE = 103;
            public const int CALENDAR_INSTANCE_CREATE = 106;
            public const int CALENDAR_INSTANCE_UPDATE = 107;
            public const int CALENDAR_MONTH_CREATE = 108;
            public const int CALENDAR_MONTH_UPDATE = 109;
            public const int SPECIAL_DAY_CREATE = 110;
            public const int SPECIAL_DAY_EDIT = 111;
            public const int WORKING_DAY_CREATE = 112;
            public const int WORK_SHIFT_CREATE = 114;
            public const int WORK_SHIFT_EDIT = 115;
            public const int ORGANISATION_STRUCTURE_DELETE = 116;
            public const int ATTENDANCE_LOG_CREATE = 118;
            public const int ATTENDANCE_LOG_UPDATE = 119;
            public const int JOB_CREATE = 120;
            public const int STAFF_CREATE = 121;
            public const int JOB_POSITION_CREATE = 122;
            public const int JOB_POSITION_UPDATE = 123;
            public const int JOB_UPDATE = 124;
            public const int JOB_POSITION_REQUEST_CREATE = 125;
            public const int JOB_POSITION_REQUEST_UPDATE = 126;
            public const int ORGANISATION_STRUCTURE_PARENT_UPDATE = 127;
            public const int GROUPS_CREATE = 128;
            public const int GROUPS_UPDATE = 129;
            public const int GROUPS_STAFF_ADD = 130;
            public const int GROUPS_STAFF_UPDATE = 131;
            public const int GROUPS_STAFF_REMOVE = 134;
            public const int GROUPS_SCHEDULE_CREATE = 132;
            public const int GROUPS_SCHEDULE_UPDATE = 133;
            public const int SHIFT_SCHEDULE_DAY_CREATE = 135;
            public const int SHIFT_SCHEDULE_DAY_UPDATE = 136;
            public const int ELEMENTS_CREATE = 137;
            public const int ELEMENTS_UPDATE = 138;
            public const int BREAK_TIMES_CREATE = 139;
            public const int BREAK_TIMES_UPDATE = 140;
            public const int ORGANISATION_STRUCTURE_SCHEDULE_CREATE = 141;
            public const int ORGANISATION_STRUCTURE_SCHEDULE_UPDATE = 142;
            public const int BUSINESSENTITY_SCHEDULE_CREATE = 143;
            public const int BUSINESSENTITY_SCHEDULE_UPDATE = 144;
            public const int SCHEDULE_CREATE = 145;
            public const int SCHEDULE_UPDATE = 146;
            public const int SCHEDULE_ELEMENT_CREATE = 147;
            public const int SCHEDULE_ELEMENT_UPDATE = 148;
            public const int SPECIAL_DAY_TYPE_CREATE = 149;
            public const int SPECIAL_DAY_TYPE_UPDATE = 150;
            public const int SCHEDULE_OWNER_CREATE = 151;
            public const int SCHEDULE_OWNER_UDPATE = 152;
            public const int BREAK_TIMES_OWNER_CREATE = 153;
            public const int BREAK_TIMES_OWNER_UDPATE = 154;
            public const int WORKSHIFT_BREAK_TIMES_CREATE = 155;
            public const int WORKSHIFT_BREAK_TIMES_UDPATE = 156;
            public const int ATTACHED_BREAK_TIMES_CREATE = 157;
            public const int ATTACHED_BREAK_TIMES_UDPATE = 158;
            public const int LEAVE_TYPE_CREATE = 159;
            public const int LEAVE_TYPE_UDPATE = 160;
            public const int LEAVE_CREATE = 161;
            public const int LEAVE_UPDATE = 162;
            public const int LEAVE_SPEDNING_LOCATION_CREATE = 163;
            public const int LEAVE_SPEDNING_LOCATION_UPDATE = 164;
            public const int LEAVE_SET_CREATE = 165;
            public const int LEAVE_SET_UDPATE = 166;
            public const int LEAVE_SET_LEAVE_TYPE_CREATE = 167;
            public const int LEAVE_SET_LEAVE_TYPE_UDPATE = 168;
            public const int GROUP_LEAVE_SET_CREATE = 169;
            public const int GROUP_LEAVE_SET_UDPATE = 170;
            public const int JOB_LEAVE_TYPE_CREATE = 171;
            public const int JOB_LEAVE_TYPE_UDPATE = 172;
            public const int LEAVE_FORM_CREATE = 173;
            public const int LEAVE_FORM_UPDATE = 174;
            public const int NO_PAY_LEAVE_CREATE = 175;
            public const int NO_PAY_LEAVE_UPDATE = 176;

            public const int TEAM_CREATE = 177;
            public const int TEAM_UPDATE = 178;
            public const int TEAM_STAFF_CREATE = 179;
            public const int TEAM_STAFF_UPDATE = 180;
            public const int LEAVE_CHANGE_REQUEST_CREATE = 181;
            public const int LEAVE_CHANGE_REQUEST_UPDATE = 182;

            public const int OUT_OF_OFFICE_REQUEST_CREATE = 183;
            public const int OUT_OF_OFFICE_REQUEST_UPDATE = 184;

            public const int POSITION_CLASSIFICATION_CREATE = 185;
            public const int POSITION_CLASSIFICATION_UPDATE = 186;

            public const int OT_PRE_APPROVAL_TIME_CREATE = 187;
            public const int OT_PRE_APPROVAL_TIME_UPDATE = 188;

            public const int POSITION_CREATE = 189;
            public const int POSITION_UPDATE = 190;
            public const int JOB_TYPE_CREATE = 191;
            public const int JOB_TYPE_UPDATE = 192;

            public const int BASIC_SALARY_CREATE = 193;
            public const int BASIC_SALARY_UPDATE = 194;
            public const int POSITION_BASIC_SALARY_CREATE = 195;
            public const int POSITION_BASIC_SALARY_UPDATE = 196;
            public const int JOBPOSITION_BASIC_SALARY_CREATE = 197;
            public const int JOBPOSITION_BASIC_SALARY_UPDATE = 198;

            public const int DEDUCTION_TYPE_CREATE = 199;
            public const int DEDUCTION_TYPE_UPDATE = 200;

            public const int JOB_DEDUCTION_CREATE = 201;
            public const int ALLOWANCE_TYPE_CREATE = 202;
            public const int ALLOWANCE_TYPE_UPDATE = 203;
            public const int ALLOWANCE_AMOUNT_CREATE = 204;
            public const int ALLOWANCE_AMOUNT_UPDATE = 205;
            public const int JOB_ALLOWANCE_CREATE = 206;
            public const int JOB_ALLOWANCE_UPDATE = 207;
            public const int ALLOWANCE_DEPENDENCY_AMOUNT_CREATE = 208;
            public const int ALLOWANCE_DEPENDENCY_AMOUNT_UPDATE = 209;

            public const int SALARY_RATE_DEFINITION_CREATE = 210;
            public const int SALARY_RATE_DEFINITION_UPDATE = 211;
            public const int OTHER_SALARY_RATE_DEFINITION_CREATE = 212;
            public const int OTHER_SALARY_RATE_DEFINITION_UPDATE = 213;

            public const int ORGANISATION_ALLOWANCE_CREATE = 216;
            public const int ORGANISATION_ALLOWANCE_UPDATE = 217;

            public const int ALLOWANCE_DEPENDENCY_TIMEAMOUNT_CREATE = 218;
            public const int ALLOWANCE_DEPENDENCY_TIMEAMOUNT_UPDATE = 219;
            public const int DEDUCTION_TYPE_AMOUNTS_CREATE = 220;
            public const int DEDUCTION_TYPE_AMOUNTS_UPDATE = 221;
            public const int OT = 222;
            public const int STAFFSALARY_CREATE = 224;
            public const int STAFFSALARY_UPDATE = 225;


            public const int ORGANISATION_STRUCTURE_SUPERVISOR_CREATE = 226;
            public const int ORGANISATION_STRUCTURE_SUPERVISOR_UPDATE = 227;

            public const int POSITION_RANK_CREATE = 228;
            public const int POSITION_RANK_UPDATE = 229;

            public const int JOB_DEDUCTION_UPDATE = 232;

            public const int ATTENDANCE_CLIENT_INSTANCE_CREATE = 233;
            public const int ATTENDANCE_CLIENT_INSTANCE_UPDATE = 234;
            public const int ATTENDANCE_DEVICE_CREATE = 235;
            public const int ATTENDANCE_DEVICE_UPDATE = 236;
            public const int STAFF_ENROLLMENT_NUMBER_CREATE = 237;
            public const int STAFF_ENROLLMENT_NUMBER_UPDATE = 238;

            public const int ORGANISATION_PAYROLL_PERIOD_CREATE = 239;
            public const int ORGANISATION_PAYROLL_PERIOD_UPDATE = 240;
            public const int ORGANISATION_PAYROLL_PERIOD_JOBTYPE_CREATE = 241;
            public const int ORGANISATION_PAYROLL_PERIOD_JOBTYPE_UPDATE = 242;
            public const int PAYROLL_CYCLE_CREATE = 243;
            public const int PAYROLL_CYCLE_UPDATE = 244;

            public const int BULK_UPLOAD_DOCUMENT_CREATE = 245;
            public const int OFFICIAL_TRIP_LOCATION_CREATE = 246;
            public const int OFFICIAL_TRIP_LOCATION_UPDATE = 247;



            public const int ATTACHED_BREAKTIMES_DAY_CREATE = 249;
            public const int ATTACHED_BREAKTIMES_DAY_UPDATE = 250;


            public const int USER_ASSIGNED_USERGROUP_CREATE = 251;
            public const int USER_ASSIGNED_USERGROUP_UPDATE = 252;

            public const int ORGANISATION_DEDUCTION_TYPE_CREATE = 253;
            public const int ORGANISATION_DEDUCTION_TYPE_UPDATE = 254;


            public const int STAFF_PAYROLL_ITEM_CREATE = 255;
            public const int STAFF_PAYROLL_ITEM_UPDATE = 256;

            public const int STAFF_PAYROLL_ITEM_TYPE_SCHEDULE_CREATE = 257;
            public const int STAFF_PAYROLL_ITEM_TYPE_SCHEDULE_UPDATE = 258;

            public const int PAYROLL_CYCLE_LINKED_REQUEST_CREATE = 259;
            public const int PAYROLL_CYCLE_LINKED_REQUEST_UPDATE = 260;


            public const int USER_ORGANISATION_USERGROUP_CREATE = 261;
            public const int USER_ORGANISATION_USERGROUP_UPDATE = 262;

            public const int SERVICE_CREATE = 263;
            public const int SERVICE_UPDATE = 264;

            public const int DATA_CORRECTION_REQUEST_CREATE = 267;
            public const int DATA_CORRECTION_REQUEST_UPDATE = 268;

            public const int PAYROLL_ITEM_WHITELIST_CREATE = 269;
            public const int PAYROLL_ITEM_WHITELIST__UPDATE = 270;

            public const int SAP_DETAILS_CREATE = 271;
            public const int PAYROLL_ITEM_DAILY_SAPSHEET_DETAILS = 272;


            public const int REQUEST_JOB_UPDATE = 273;
            public const int REQUEST_TEAM_CREATE = 274;
            public const int REQUEST_TEAM_UPDATE = 275;

            public const int USER_PREFERENCES_CREATE = 276;
            public const int USER_PREFERENCES_UPDATE = 277;
            public const int REQUEST_JOB_CREATE = 278;

            public const int LEAVE_RELATED_DOCUMENT_CREATE = 281;
            public const int LEAVE_RELATED_DOCUMENT_DELETE = 282;
            public const int KPI_RELATED_DOCUMENT_CREATE = 283;
            public const int KPI_RELATED_DOCUMENT_DELETE = 284;


        }

        //*******************************************************************************

        #endregion Operation Log Action Types


        #region "Request Types"

        //*******************************************************************************

        public static class RequestTypes
        {
            public const int USER_REGISTRATION = 1;
            public const int PAYROLL_CYCLE = 2;
            public const int BUSINESS_ENTITIES_RELATIONS_CREATE = 16;
            public const int BUSINESS_ENTITIES_RELATIONS_DEACTIVATE = 17;
            public const int BUSINESS_ENTITIES_RELATIONS_ACTIVATE = 18;
            public const int BUSINESS_ENTITIES_RELATIONS_TERMINATE = 19;
            public const int BUSINESS_ENTITIES_RELATIONS_REACTIVATE = 20;
            public const int NEW_JOB_REQUEST = 21;
            public const int JOB_TERMINATION_REQUEST = 22;
            public const int ATTENDANCE_LOG_CHANGE_REQUEST = 24;
            public const int ATTENDANCE_LOG_REQUEST = 25;
            public const int JOB_RESIGNATION_REQUEST = 26;
            public const int JOB_RETIREMENT_REQUEST = 27;
            public const int ORGANISATION_STRUCTURE_REQUEST = 31;
            public const int JOB_POSITION_REQUEST = 32;
            public const int JOB_POSITION_TERMINATION_REQUEST = 33;
            public const int ANNUAL_LEAVE_REQUEST = 34;
            public const int CHILDCARE_LEAVE_REQUEST = 35;
            public const int MATERNITY_LEAVE_REQUEST = 36;
            public const int PATERNITY_LEAVE_REQUEST = 37;
            public const int CIRCUMCISION_LEAVE_REQUEST = 38;
            public const int HAJJ_LEAVE_REQUEST = 39;
            public const int FAMILY_LEAVE_REQUEST = 40;
            public const int SICK_LEAVE_REQUEST = 41;
            public const int BEFORE_BIRTH_MATERNITY_LEAVE_REQUEST = 42;
            public const int AFTER_BIRTH_MATERNITY_LEAVE_REQUEST = 43;
            public const int MISCARRIAGE_BIRTH_MATERNITY_LEAVE_REQUEST = 44;
            public const int STILL_BIRTH_MATERNITY_LEAVE_REQUEST = 45;
            public const int NOPAY_LEAVE_REQUEST = 46;
            public const int JOB_TEMPORARY_TERMINATION_REQUEST = 47;
            public const int LEAVE_CHANGE_REQUEST = 48;
            public const int OUT_OF_OFFICE_REQUEST = 50;
            public const int OT_PRE_APPROVAL_REQUEST = 51;
            public const int OT_REQUEST = 52;
            public const int ALLOWANCE_TYPES_REQUEST = 53;
            public const int STAFF_ALLOWANCE_REQUEST = 54;
            public const int SALARY_RATES_DEFINITION_REQUEST = 55;
            public const int OFFICIAL_TRIP = 57;
            public const int STAFF_ASSIGNED_DEDUCTION_REQUEST = 59;
            public const int DEDUCTION_TYPES_REQUEST = 62;
            public const int STAFF_PAYROLL_ITEM_TYPE_REQUEST = 63;
            public const int WORK = 66;
            public const int TASK = 67;
            public const int JOB_TERMINATION_DUE_TO_PROMOTION_REQUEST = 71;
            public const int Attendance_Log_Mode_Change_Request = 76;
            public const int JOB_TERMINATION_DUE_TO_CONTRACT_EXPIRATION = 77;
            public const int JOB_TERMINATION_DURING_PROBATION = 78;

            //ADDED BY AHMED SHARIEPH 2019
            public const int JOB_TERMINATION_TERMINATED = 93;
            public const int JOB_TERMINATION_DUE_TO_OTHER_REASON = 94;


        }

        //*******************************************************************************

        #endregion "Request Types"

        #region Job States
        public static class JobStates
        {
            public const int INCOMPLETE = 1;
            public const int PENDING_VERIFICATION = 2;
            public const int PENDING_APPROVAL = 3;
            public const int APPROVED = 4;
            public const int CANCELLED = 5;
            public const int REJECTED = 6;
            public const int RESIGNED = 7;
            public const int RETIRED = 8;
            public const int TERMINATED = 15;
            public const int TERMINATED_DUE_TO_PROMOTION = 16;
            public const int TERMINATED_DUE_TO_CONTRACT_EXPIRATION = 17;
            public const int TERMINATED_DURING_PROBATION = 18;
            public const int TERMINATED_OTHER = 19;
        }

        #endregion Job States

        #region Leave Types

        public static class LeaveTypes
        {
            public const int ANNUAL_LEAVE = 1;
            public const int CHILDCARE_LEAVE = 2;
            public const int MATERNITY_LEAVE = 3;
            public const int PATERNITY_LEAVE = 5;
            public const int CIRCUMCISION_LEAVE = 6;
            public const int HAJJ_LEAVE = 7;
            public const int FAMILY_LEAVE = 8;
            public const int SICK_LEAVE = 9;
            public const int BEFORE_BIRTH_MATERNITY_LEAVE = 10;
            public const int AFTER_BIRTH_MATERNITY_LEAVE = 11;
            public const int MISCARRIAGE_BIRTH_MATERNITY_LEAVE = 12;
            public const int STILL_BIRTH_MATERNITY_LEAVE = 14;
            public const int NO_PAY_LEAVE = 15;
            public const int OFFICIAL_TRIP = 31;
            public const int PAID_STUDY_LEAVE = 36;
            public const int COVID_QUARANTINE_LEAVE = 52;

        }


        #endregion Leave Types

        #region "AuditLog Data Types"

        public static class AuditLogDataTypes
        {
            public const int Reservation = 1;
            public const int Nationality = 2;
            public const int Occupancy = 3;
            public const int Facility = 4;
            public const int Service = 5;
            public const int ImmigrationData = 6;
            public const int Organization = 7;
            public const int BussinessEntityContactInformations = 8;
            public const int BussinessEntityRelations = 9;
            public const int BusinessEntityRelationLinkedServices = 10;
            public const int Individuals = 11;
            public const int Passport = 12;
            public const int IDCard = 13;
            public const int FacilityRegistrationContacts = 14;
            public const int Safari = 15;
            public const int Resort = 16;
            public const int Hotel = 17;
            public const int GuestHoust = 18;
            public const int YachtMarina = 19;
            public const int DiveSchool = 20;
            public const int PicnicIsland = 21;
            public const int TourOperators = 22;
            public const int TourOperatorContacts = 23;
            public const int TourOperatorService = 24;
            public const int Requests = 25;
            public const int Documents = 26;
            public const int ParentDataCollection = 27;
            public const int FacilityRegistrationTypes = 29;
            public const int Users = 28;
            public const int USER_ROLES = 29;
            public const int TourOperatorOnline = 31;
            public const int TourOperatorOnlineService = 32;
            public const int TourOperatorOnlineContact = 33;
            public const int FacilityRegistrationAddress = 34;
            public const int FacilityOwnerUpdate = 36;
            public const int FacilityCurrentOperatorUpdate = 37;
            public const int FacilityManagementUpdate = 38;
            public const int FacilityLesseeUpdate = 39;
            public const int FacilitySubLesseeUpdate = 40;
            public const int BusinessEntityMailingAddress = 42;
            public const int BusinessEntityRegisteredAddress = 43;
            public const int BusinessEntityPermanentAddress = 44;
            public const int HumanResourceManagementIslands = 45;
            public const int HumanResourceManagementIslandsAttachIslands = 46;
            public const int Address = 47;
            public const int UserServiceRole = 48;
            public const int DataSubmissionNotification = 49;
            public const int CharterLicense = 50;
            public const int CruisePermit = 51;
            public const int Payment = 52;
            public const int PaymentItem = 53;
            public const int BusinessEntityCalendar = 54;
            public const int Calendar = 55;
            public const int CalendarType = 56;
            public const int OrganisationStructureCalendar = 58;
            public const int Month = 57;
            public const int CalendarInstance = 59;
            public const int CalendarMonth = 60;
            public const int SpecialDay = 61;
            public const int WorkingDay = 62;
            public const int WorkShift = 63;
            public const int OrganisationStructureDraft = 64;
            public const int OrganisationStructureHistory = 65;
            public const int AttendanceLogChangeRequest = 66;
            public const int AttendanceLog = 67;
            public const int AttendanceLogRequest = 68;
            public const int Staff = 69;
            public const int Job = 70;
            public const int JobPositions = 73;
            public const int Positions = 74;
            public const int JobPositionRequests = 75;
            public const int OrganisationStructure = 76;
            public const int Groups = 77;
            public const int GroupStaffs = 78;
            public const int OrganisationStructureMailingAddress = 80;
            public const int AttendanceClients = 81;
            public const int AttendanceDevices = 82;
            public const int OrganisationStructureSchedule = 83;
            public const int BusinessEntitySchedule = 84;
            public const int GroupSchedule = 85;
            public const int Schedule = 86;
            public const int ScheduleElement = 87;
            public const int Element = 88;
            public const int ShiftScheduleDay = 89;
            public const int SpecialDayType = 90;
            public const int BreakTime = 91;
            public const int ScheduleOwners = 92;
            public const int BreakTimeOwners = 93;
            public const int WorkShiftBreakTime = 94;
            public const int AttachedBreakTime = 95;
            public const int LeaveType = 96;
            public const int LeaveSet = 97;
            public const int LeaveSetLeaveType = 98;
            public const int GroupLeaveSet = 99;
            public const int JobLeaveType = 100;
            public const int Team = 101;
            public const int TeamStaff = 102;
            public const int OTPreApprovals = 103;
            public const int OTPreApprovalTimes = 104;
            public const int Leaves = 105;
            public const int BasicSalary = 106;
            public const int JobPositionBasicSalary = 107;
            public const int PositionBasicSalary = 108;
            public const int OTRequest = 109;
            public const int DeductionTypes = 110;
            public const int OT = 111;
            public const int AllowanceTypes = 113;
            public const int AllowanceAmount = 114;
            public const int JobAllowance = 115;
            public const int SalaryRateDefinition = 116;
            public const int OtherSalaryRateDefinition = 117;
            public const int OrganisationAllowance = 119;

            public const int OrganisationStructureSupervisor = 121;
            public const int OrganisationPayrollPeriod = 122;
            public const int OrganisationPayrollPeriodJobtype = 123;
            public const int PayrollCycle = 124;
            public const int OfficialTripLocation = 125;
            public const int StaffAssignedDeductions = 126;
            public const int AttachedBreakTimesDay = 127;
            public const int UserAssignedUserGroups = 128;
            public const int UserGroup = 129;
            public const int DataCorrectionRequest = 130;
            public const int DataCorrectionAttributeValue = 131;
            public const int PayrollItemProcessingWhiteList = 132;
            public const int StaffPayrollItemType = 133;
            public const int StaffPayrollItemTypeAmount = 134;
            public const int StaffPayrollItemTypeSchedule = 135;
            public const int RequestJobs = 138;
            public const int RequestTeams = 139;
            public const int LeaveConfigurableValue = 136;
            public const int LeaveChangeRequest = 137;
            public const int GroupTypes = 141;

        }

        #endregion "AuditLog Data Types"


        #region "AuditLog Action Types"

        public static class AuditLogActionTypes
        {
            public const int StateChange = 1;
            public const int Edit = 2;
            public const int ApplicationStageEdit = 3;
            public const int View = 4;
            public const int Add = 5;
        }

        #endregion "AuditLog Action Types"

        #region Leave States

        public static class LeaveStates
        {
            public const int INCOMPLETE = 1;
            public const int PENDING_VERIFICATION = 2;
            public const int PENDING_APPROVAL = 3;
            public const int APPROVED = 4;
            public const int REJECTED = 5;
            public const int CANCELLED = 6;
            public const int PENDING_HR_HEAD_APPROVAL = 7;
            public const int PENDING_HOD_APPROVAL = 8;


        }

        // Leave balance deduction policy
        // Deduct on create starting from this date, deduct on approve before that
        // Set to a non-working day, not working day like Thursday
        public static class LeaveBalanceDeductionPolicy
        {
            public const int YEAR = 2017;
            public const int MONTH = 1;
            public const int DAY = 1;
        }

        #endregion Leave States

    }
}

