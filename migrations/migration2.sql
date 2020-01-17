DROP VIEW inbound;

create view inbound as
SELECT stock.gtin_cd, stock.w_id, stock.hld, gcp.gcp_cd,
gcp.gln_nm, gcp.gln_addr_02, gcp.gln_addr_03,
gcp.gln_addr_04, gcp.gln_addr_postalcode, gcp.gln_addr_city,
gcp.contact_tel, gcp.contact_mail, gtin.gtin_cd, gtin.gtin_nm, gtin.m_g, gtin.l_th, gtin.ds, gtin.min_qt
FROM stock
INNER JOIN gtin ON stock.gtin_cd = gtin.gtin_cd
INNER JOIN gcp on gcp.gcp_cd = gtin.gcp_cd;

ALTER TABLE stock ADD COLUMN product_gtin VARCHAR(13);
UPDATE stock SET product_gtin = (SELECT gtin_cd from gtin WHERE stock.p_id = gtin.p_id);

----DROP FOREIGN KEY on stock
ALTER TABLE stock
DROP CONSTRAINT stock_p_id_pkey;

---DROP PRIMARY KEY on stock
ALTER TABLE stock
DROP CONSTRAINT stock_p_id_fkey;

---DROP p_id COLUMN on stock
ALTER TABLE stock
DROP COLUMN p_id

---CREATE PRIMARY KEY (gtin_cd, w_id) on stock
ALTER TABLE stock
ADD CONSTRAINT stock_pkeys
PRIMARY KEY (w_id);

------CREATE FOREIGN KEY TO gtin TABLE on stock
ALTER TABLE stock
ADD FOREIGN KEY (gtin_cd) REFERENCES gtin (gtin_cd);
------
ALTER TABLE gtin
ADD FOREIGN KEY (gcp_cd) REFERENCES gcp (gcp_cd);

DROP INDEXES from gtin
DROP PRIMARY KEY from gtin
DROP COLUMN p_id
