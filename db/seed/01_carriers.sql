-- Seeds Receiving's carrier master (receiving.carriers).
--
-- Safe to re-run: a carrier already present, matched on its SCAC, keeps its carrier_id and takes the name
-- given here. Carriers removed from this file are not deleted from the database.
--
-- carrier_id is fixed once assigned, because receipts store it. Add a carrier at the end with the next id;
-- never renumber.
--
-- Based on https://hub.acctivate.com/articles/shipping/scac-codes, without the carriers that no longer
-- operate under the listed name. HJBT and WERN were added from memory and are not confirmed against a source.

BEGIN;

INSERT INTO receiving.carriers (carrier_id, scac_code, carrier_name)
VALUES
    (1,  'ABFS', 'ABF Freight'),
    (2,  'ALLV', 'Allied Van Lines'),
    (3,  'CBNP', 'Carbonite Logistics'),
    (4,  'CMDU', 'CMA CGM'),
    (5,  'COSU', 'Coscon Container Lines'),
    (6,  'COTO', 'Country Transport'),
    (7,  'CSXT', 'CSX Transportation'),
    (8,  'DMLI', 'Diamond Logistics'),
    (9,  'DOLR', 'Dot-Line Transportation'),
    (10, 'EGLV', 'Evergreen'),
    (11, 'ELOI', 'Ease Logistics'),
    (12, 'EXLA', 'Estes Express Lines'),
    (13, 'EXPD', 'Expeditors International Ocean'),
    (14, 'FDCC', 'FedEx Custom Critical'),
    (15, 'FDEG', 'FedEx Ground'),
    (16, 'FDEN', 'FedEx (Air)'),
    (17, 'FEXF', 'FedEx Freight'),
    (18, 'FLJF', 'FLT Logistics LLC'),
    (19, 'FXFE', 'FedEx LTL Freight East'),
    (20, 'FXFW', 'FedEx LTL Freight West'),
    (21, 'FXNL', 'FedEx Freight National'),
    (22, 'GMLS', 'R and L Carriers'),
    (23, 'HJBT', 'J.B. Hunt'),
    (24, 'HLCU', 'Hapag-Lloyd'),
    (25, 'HOYO', 'HOYER Deep Sea'),
    (26, 'INML', 'Intermodal Services'),
    (27, 'KHNN', 'Kuehne + Nagel'),
    (28, 'LDYN', 'Logistic Dynamics, Inc.'),
    (29, 'MAEU', 'Maersk Line'),
    (30, 'MCPU', 'MCC Transport Singapore Pte Ltd.'),
    (31, 'MGMC', 'MGM Transport'),
    (32, 'MSCU', 'Mediterranean Shipping Company'),
    (33, 'MTEN', 'Marten Transport Ltd.'),
    (34, 'NAFT', 'National Freight'),
    (35, 'NXPL', 'National Xpress Logistics'),
    (36, 'ODFL', 'Old Dominion Freight Line'),
    (37, 'OOLU', 'Orient Overseas Container Line'),
    (38, 'PAYL', 'Paystar Logistics'),
    (39, 'PDME', 'Piedmont Express'),
    (40, 'PFLP', 'Pacific Logistics Corp'),
    (41, 'PLGQ', 'Precision Logistics Group'),
    (42, 'RDSS', 'Rail Delivery Services, Inc.'),
    (43, 'SAIA', 'Saia'),
    (44, 'SCNN', 'Schneider National'),
    (45, 'SNCY', 'Schneider National'),
    (46, 'SNLU', 'Schneider National'),
    (47, 'SWFT', 'Swift Transportation'),
    (48, 'TCKM', 'Truckers America'),
    (49, 'TPNW', 'Triple A Logistics'),
    (50, 'TRKU', 'Turkon Line Inc'),
    (51, 'UDRY', 'USAW - USA Logistics'),
    (52, 'UPSC', 'United Parcel Service'),
    (53, 'UPSN', 'United Parcel Service'),
    (54, 'UPSS', 'United Parcel Service'),
    (55, 'UPSZ', 'United Parcel Service'),
    (56, 'UQEP', 'Unique Express'),
    (57, 'USAU', 'U.S. Government'),
    (58, 'USAX', 'U.S. Government'),
    (59, 'USIT', 'USA Truck'),
    (60, 'USNW', 'U.S. Northwest Express'),
    (61, 'USPS', 'United States Postal Service'),
    (62, 'USXI', 'U.S. Xpress'),
    (63, 'VLOQ', 'Vanedge Logistics'),
    (64, 'VSRD', 'Vistar Transportation Ltd.'),
    (65, 'WERN', 'Werner Enterprises'),
    (66, 'XPOL', 'XPO Logistics')
ON CONFLICT (scac_code) DO UPDATE
    SET carrier_name = EXCLUDED.carrier_name;

COMMIT;
