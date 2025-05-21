<?xml version='1.0' encoding='UTF-8'?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
  <!-- Identity transform -->
  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the male_names element and add new names -->
  <xsl:template match="Culture[@id='nord']/male_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=6dA858a00}Aksel</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=27a1b879d}Bjorn</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=260706E3e}Olafr</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=5817d7796}Helgerad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=7D409224a}Ole</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=76c5f78c6}Gundar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=24B0f993c}Haraldr</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=5b2040ce5}Hjalmar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=c2a4a3811}Njal</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=608b67cDF}Leif</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=9b059fb5d}Lethwyn</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=3Fa118666}Guthric</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the female_names element and add new names -->
  <xsl:template match="Culture[@id='nord']/female_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=685E8521a}Astrith</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=7518f598e}Kara</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a3452cd95}Elsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=1260df227}Kelsea</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=f68775050}Freja</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=66Af5B023}Annika</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=9e33659f4}Ulla</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=2d5780DF4}Kay</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to add notable_and_wanderer_templates element within the nord culture if it doesn't already exist -->
  <xsl:template match="Culture[@id='nord']">
    <xsl:copy>
      <xsl:apply-templates select="@*"/>
      <xsl:if test="not(notable_and_wanderer_templates)">
        <notable_and_wanderer_templates>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_0b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_2b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_3b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_3c</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_sturgia_headman_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_sturgia_headman_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_sturgia_headman_3</xsl:attribute>
          </template>
        </notable_and_wanderer_templates>
      </xsl:if>
      <xsl:apply-templates select="node()"/>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="Culture[@id='vakken']/male_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=b9bC51FA1}Tapio</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=57caa0717}Tuisko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=7B625e93e}Aamu</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=255b7cDB0}Aarre</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=fc0e0aCAA}Aimo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a696B7b7D}Airi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=48d54b745}Arvo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=dd6636f32}Eero</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=ea4af1D12}Heino</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=d972Eab13}Hanno</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=78d76841D}Liro</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=316a1933a}Jalo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=548e9F418}Iskko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=eFF794a56}Joukko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=1292cE85D}Kaapo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=1Ab3b7677}Keijo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a1E9ad3f0}Kauko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=95801199d}Lari</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the female_names element and add new names -->
  <xsl:template match="Culture[@id='vakken']/female_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=16aA03c15}Laila</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a97987E71}Aira</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=f32Dc5D55}Alli</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=ec3524082}Ella</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=bEEB79895}Elina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a3452cd95}Elsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=282b0aA21}Fanni</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=67d912f52}Hanna</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=9DFA192f3}Heidi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=f32Dc5D55}Alli</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=ec3524082}Ella</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=bEEB79895}Elina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a3452cd95}Elsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=282b0aA21}Fanni</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=4FF4f13f8}Likka</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=07449560B}Lina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=8BCf1246F}Liris</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=45c4d5C16}Lahja</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=d02F4cE57}Kukka</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=7BEbeBaD1}Lauri</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to add notable_and_wanderer_templates element within the nord culture if it doesn't already exist -->
  <xsl:template match="Culture[@id='vakken']">
    <xsl:copy>
      <xsl:apply-templates select="@*"/>
      <xsl:if test="not(notable_and_wanderer_templates)">
        <notable_and_wanderer_templates>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_wanderer_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_0b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_2b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_3b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_3c</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_notable_sturgia_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_sturgia_headman_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_sturgia_headman_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">NPCCharacter.spc_sturgia_headman_3</xsl:attribute>
          </template>
        </notable_and_wanderer_templates>
      </xsl:if>
      <xsl:apply-templates select="node()"/>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the male_names element and add new names for darshi culture -->
  <xsl:template match="Culture[@id='darshi']/male_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=f1b44ff5b}Bahram</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=22710985a}Shahbur</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=507F88A72}Sargon</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=f207fb948}Hormizhd</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=8FC990C53}Ardashar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=3f4eE6C26}Kavadh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=dd06088F2}Khosrau</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=2fc6c761e}Balash</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=96b0Ea3e5}Sarhad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=cFaBe1153}Esarhaddon</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=8864a0660}Oshalimahun</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=969d55634}Zar-Adan-Ekhi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=855dFf228}Nabu-Ikbi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=76BF329F5}Kudur-Yutsur</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=455F889F6}Nabu-Ekhi-Erba</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=c4D50bfF2}Kanun</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=33Fc2Ad51}Mannu-Ki-Ashur</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=187C38997}Namtar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=bE8afdBc0}Nergal-Edir</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=29752AdBd}Nergal-Sar-Ussr</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=c7aEb4EB0}Nergiglissar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=35036f0F8}Nidintabel</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=5C59c99b2}Shagaraktiyash</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=450053329}Sisuthrus</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a57FFfCF9}Kasu-Nadin-Ahu</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=661C73963}Merodak-Mu-Basa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=b62Cd33B6}Misa-Nana-Kal-Ammi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=fb1cE4353}Mulaghunnuna</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=652123587}Ul-Khum-Zabu</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=1C34B560D}Idadu</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=94742db9f}Nabonahid</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=730050e69}Charbil</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=e4b9C4Be3}Sayad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=6eB447007}Khamis</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=091dA7Aa8}Zeyno</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=5e9C9EF21}Adrahasis</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=7A56a23D1}Annunitum</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=c05444f7b}Akku</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=2eDdDE9Ce}Furqan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=28B7548Ae}Farzad</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the female_names element and add new names for darshi culture -->
  <xsl:template match="Culture[@id='darshi']/female_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=b71f5636E}Hedu</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=eB971de20}Sharukina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=95aD68F40}Ashurina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=dc1a8bA53}Arbella</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=a1C811992}Larsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=22F6c2C0a}Deqlat</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=40Abd6787}Sahdah</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=31c50E08c}Nviya</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=0A39400C9}Nahrain</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=1ac800c1a}Tauno</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=112546587}Khaya</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=56BF418FE}Warda</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=f88878A0e}Ramina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=1e8A46b6b}Warduni</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=bf341d85f}Ilona</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=ec3FA7F20}Neesan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=b0148dc1a}Nuri-Shapira</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=f5D463bF8}Dawa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=bd103842c}En-hedu-anna</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=cf114b874}Naram-sin</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=2b5238671}Tashlultum</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=38F0f3e89}Agusaya</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=878eea0DE}Raga</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to add notable_and_wanderer_templates element within the darshi culture if it doesn't already exist -->
  <xsl:template match="Culture[@id='darshi']">
    <xsl:copy>
      <xsl:apply-templates select="@*"/>
      <xsl:if test="not(notable_and_wanderer_templates)">
        <notable_and_wanderer_templates>
          <template name="NPCCharacter.spc_wanderer_aserai_0"/>
          <template name="NPCCharacter.spc_wanderer_aserai_1"/>
          <template name="NPCCharacter.spc_wanderer_aserai_2"/>
          <template name="NPCCharacter.spc_wanderer_aserai_3"/>
          <template name="NPCCharacter.spc_wanderer_aserai_4"/>
          <template name="NPCCharacter.spc_wanderer_aserai_5"/>
          <template name="NPCCharacter.spc_wanderer_aserai_6"/>
          <template name="NPCCharacter.spc_wanderer_aserai_7"/>
          <template name="NPCCharacter.spc_wanderer_aserai_8"/>
          <template name="NPCCharacter.spc_wanderer_aserai_9"/>
          <template name="NPCCharacter.spc_wanderer_aserai_10"/>
          <template name="NPCCharacter.spc_notable_aserai_0"/>
          <template name="NPCCharacter.spc_notable_aserai_0b"/>
          <template name="NPCCharacter.spc_notable_aserai_1"/>
          <template name="NPCCharacter.spc_notable_aserai_1b"/>
          <template name="NPCCharacter.spc_notable_aserai_2"/>
          <template name="NPCCharacter.spc_notable_aserai_2b"/>
          <template name="NPCCharacter.spc_notable_aserai_3"/>
          <template name="NPCCharacter.spc_notable_aserai_3b"/>
          <template name="NPCCharacter.spc_notable_aserai_4"/>
          <template name="NPCCharacter.spc_notable_aserai_5"/>
          <template name="NPCCharacter.spc_notable_aserai_6"/>
          <template name="NPCCharacter.spc_notable_aserai_7"/>
          <template name="NPCCharacter.spc_notable_aserai_8"/>
          <template name="NPCCharacter.spc_notable_aserai_9"/>
          <template name="NPCCharacter.spc_notable_aserai_10"/>
          <template name="NPCCharacter.spc_notable_aserai_11"/>
          <template name="NPCCharacter.spc_notable_aserai_12"/>
          <template name="NPCCharacter.spc_aserai_headman_1"/>
          <template name="NPCCharacter.spc_aserai_headman_2"/>
          <template name="NPCCharacter.spc_aserai_headman_3"/>
        </notable_and_wanderer_templates>
      </xsl:if>
      <xsl:apply-templates select="node()"/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
<!-- <SPCultures>
    <Culture
		id="nord"
		name="{=sYFaoW7G}Nord"
		is_main_culture="false">
		<male_names>
			<name name="{=ig8zAo8f}Ori" />
            <name name="{=ig8zAo8f}Squi" />
            <name name="{=ig8zAo8f}Abagi" />
            <name name="{=ig8zAo8f}Jorolf" />
            <name name="{=ig8zAo8f}Gyak" />
		</male_names>
		<female_names>
			<name
				name="{=L0fuYckc}Friga" />
		</female_names>
	</Culture>
</SPCultures> -->
