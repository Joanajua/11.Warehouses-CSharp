create view inbound as
SELECT stock.p_id, stock.w_id, stock.hld, gcp.gcp_cd,
gcp.gln_nm, gcp.gln_addr_02, gcp.gln_addr_03,
gcp.gln_addr_04, gcp.gln_addr_postalcode, gcp.gln_addr_city,
gcp.contact_tel, gcp.contact_mail, gtin.gtin_cd, gtin.gtin_nm, gtin.m_g, gtin.l_th, gtin.ds, gtin.min_qt
FROM stock
INNER JOIN gtin ON stock.p_id = gtin.p_id
INNER JOIN gcp on gcp.gcp_cd = gtin.gcp_cd;