<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
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
        <xsl:attribute name="name">{=IMFRDC76966f3}Aksel</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD359FaFED}Bjorn</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDBf075cfd}Olafr</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDe00ca140}Helgerad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDBd7CBDc7}Ole</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD5b486511}Gundar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD4d0276f3}Haraldr</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD95774021}Hjalmar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD765a2560}Njal</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD058dC9DC}Leif</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1f95d2aF}Lethwyn</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDBb3e156E}Guthric</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the female_names element and add new names -->
  <xsl:template match="Culture[@id='nord']/female_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=IMFRDc6F65C8b}Astrith</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDca142107}Kara</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD8b34F8c1}Elsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD8279B0e8}Kelsea</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDE70c0873}Freja</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD0353c396}Annika</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD8cD6e136}Ulla</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD5a2C1aC6}Kay</xsl:attribute>
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
            <xsl:attribute name="name">{=IMFRD969e7427}NPCCharacter.spc_wanderer_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDF728E8eE}NPCCharacter.spc_wanderer_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD10165B18}NPCCharacter.spc_wanderer_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDd8B64833}NPCCharacter.spc_wanderer_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5C8b6c40}NPCCharacter.spc_wanderer_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD949e2016}NPCCharacter.spc_wanderer_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5094Ae13}NPCCharacter.spc_wanderer_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD469e7102}NPCCharacter.spc_wanderer_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5951F891}NPCCharacter.spc_wanderer_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDFdC9e507}NPCCharacter.spc_wanderer_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDEf35B4f4}NPCCharacter.spc_notable_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD73b4Aaa9}NPCCharacter.spc_notable_sturgia_0b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD76C3822d}NPCCharacter.spc_notable_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDAf0242c1}NPCCharacter.spc_notable_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD77EB773a}NPCCharacter.spc_notable_sturgia_2b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD58E85dAc}NPCCharacter.spc_notable_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD0682ccE9}NPCCharacter.spc_notable_sturgia_3b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD8a603957}NPCCharacter.spc_notable_sturgia_3c</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD4EC1e9da}NPCCharacter.spc_notable_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD41655f4D}NPCCharacter.spc_notable_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD16B829c2}NPCCharacter.spc_notable_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDeeAa5682}NPCCharacter.spc_notable_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD872132ca}NPCCharacter.spc_notable_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD202f29C8}NPCCharacter.spc_notable_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD14Fa45E7}NPCCharacter.spc_notable_sturgia_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD72827f26}NPCCharacter.spc_sturgia_headman_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDE1662ba8}NPCCharacter.spc_sturgia_headman_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD33A610C3}NPCCharacter.spc_sturgia_headman_3</xsl:attribute>
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
        <xsl:attribute name="name">{=IMFRD01919276}Tapio</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD15c11cd5}Tuisko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6E958d65}Aamu</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD2Dc11236}Aarre</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDa4DF8E30}Aimo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD5D9bD15f}Airi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDD53bA851}Arvo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6BDd2fD0}Eero</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1e0aA70d}Heino</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDD57BBE05}Hanno</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD692F4f17}Liro</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDc2Bc2A94}Jalo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDfeEB7498}Iskko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD553E87A8}Joukko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1022de37}Kaapo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6a504995}Keijo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD2eB5768d}Kauko</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDD76CE56C}Lari</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the female_names element and add new names -->
  <xsl:template match="Culture[@id='vakken']/female_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=IMFRD339845db}Laila</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD4a7D0b9E}Aira</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD26726c33}Alli</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDcc00e6e0}Ella</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD39B6c393}Elina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD8b34F8c1}Elsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDe00d75bE}Fanni</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD3cBB745A}Hanna</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD239A50a5}Heidi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD26726c33}Alli</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDcc00e6e0}Ella</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD39B6c393}Elina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD8b34F8c1}Elsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDe00d75bE}Fanni</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD872eAba9}Likka</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD8651b4f9}Lina</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD27303e85}Liris</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD75E2c8b3}Lahja</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDbe75DBf5}Kukka</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDf2C01cc0}Lauri</xsl:attribute>
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
            <xsl:attribute name="name">{=IMFRD969e7427}NPCCharacter.spc_wanderer_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDF728E8eE}NPCCharacter.spc_wanderer_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD10165B18}NPCCharacter.spc_wanderer_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDd8B64833}NPCCharacter.spc_wanderer_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5C8b6c40}NPCCharacter.spc_wanderer_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD949e2016}NPCCharacter.spc_wanderer_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5094Ae13}NPCCharacter.spc_wanderer_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD469e7102}NPCCharacter.spc_wanderer_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5951F891}NPCCharacter.spc_wanderer_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDFdC9e507}NPCCharacter.spc_wanderer_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDEf35B4f4}NPCCharacter.spc_notable_sturgia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD73b4Aaa9}NPCCharacter.spc_notable_sturgia_0b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD76C3822d}NPCCharacter.spc_notable_sturgia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDAf0242c1}NPCCharacter.spc_notable_sturgia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD77EB773a}NPCCharacter.spc_notable_sturgia_2b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD58E85dAc}NPCCharacter.spc_notable_sturgia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD0682ccE9}NPCCharacter.spc_notable_sturgia_3b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD8a603957}NPCCharacter.spc_notable_sturgia_3c</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD4EC1e9da}NPCCharacter.spc_notable_sturgia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD41655f4D}NPCCharacter.spc_notable_sturgia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD16B829c2}NPCCharacter.spc_notable_sturgia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDeeAa5682}NPCCharacter.spc_notable_sturgia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD872132ca}NPCCharacter.spc_notable_sturgia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD202f29C8}NPCCharacter.spc_notable_sturgia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD14Fa45E7}NPCCharacter.spc_notable_sturgia_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD72827f26}NPCCharacter.spc_sturgia_headman_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDE1662ba8}NPCCharacter.spc_sturgia_headman_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD33A610C3}NPCCharacter.spc_sturgia_headman_3</xsl:attribute>
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
        <xsl:attribute name="name">{=IMFRDF94Fa914}Bahramshir</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD38473404}Shahrvand</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD3aB81Dc1}Faramarzad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD2bB57a98}Ardeshiron</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDCb9780a4}Darayashan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD606F02DE}Hoshmandar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD186305AA}Turanfar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDA50def71}Keyvanrooz</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD899b5366}Shaygaran</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6998B2Ff}Vizaresht</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6998B2Ff}Vizaresht</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD9694dC96}Zandavash</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6D92Ab0f}Mehrdast</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD3EE66065}Sorushan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDe6F8E5aF}Ormazdian</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDC3A062ce}Vahramid</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDFd2d1fcA}Rustamdar</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD31d8a311}Farshadmir</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDe6D4cA3c}Kavehzad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD06d0d6CE}Borzuya</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDee9a46f0}Javanshir</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDC34Eb9fE}Surenbad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD3dB60b71}Shapurkan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDe858e91B}Niyozad</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDaeCD5afC}Faravand</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD46369af7}Zartoshan</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the female_names element and add new names for darshi culture -->
  <xsl:template match="Culture[@id='darshi']/female_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=IMFRD951262dd}Parishanaz</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD68039589}Rokhsareh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDBbB88DfD}Mehrnazir</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD10FcB7d4}Sorayanda</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDAfEBCfe2}Golmira</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1428Bf49}Zhalehzan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD98C07651}Nazbanou</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDf6490ab8}Khosrojin</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDe2B73261}Farvaneh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD02241e75}Shahrdokht</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD0dAb9D31}Yasamehr</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDa2F83FfF}Anahita</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD7C8012cc}Shirinaz</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD5e036ae5}Dilafruz</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDA9A635ee}Mahrokh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1A6cFEe1}Shetareh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD4Ea173f4}Rozhimeh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD5CE05eab}Shahdaneh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDac407c1c}Nozhan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD4467e755}Veshtara</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD9f1bfE0C}Taraneh</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDD3DC833F}Banujahan</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD19a91259}Zarnaz</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDA7EC8b6F}Shahreza</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD3A50606c}Elhamis</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD4f66010E}Nasimeh</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="Culture[@id='darshi']">
    <xsl:copy>
      <xsl:apply-templates select="@*"/>
      <xsl:if test="not(notable_and_wanderer_templates)">
        <notable_and_wanderer_templates>
          <template>
            <xsl:attribute name="name">{=IMFRDa0633732}NPCCharacter.spc_wanderer_darshi_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDc8B30308}NPCCharacter.spc_wanderer_darshi_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD93F56F76}NPCCharacter.spc_wanderer_darshi_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD4c4ce209}NPCCharacter.spc_wanderer_darshi_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDA70c042F}NPCCharacter.spc_wanderer_darshi_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD392fcF7B}NPCCharacter.spc_wanderer_darshi_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDD7837412}NPCCharacter.spc_wanderer_darshi_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD14a58EfE}NPCCharacter.spc_wanderer_darshi_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5e4df49E}NPCCharacter.spc_wanderer_darshi_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDc68d33cF}NPCCharacter.spc_wanderer_darshi_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD6451B57e}NPCCharacter.spc_wanderer_darshi_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDecF555b8}NPCCharacter.spc_notable_darshi_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5BDcEaAF}NPCCharacter.spc_notable_darshi_0b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDe2896e70}NPCCharacter.spc_notable_darshi_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD850e9be4}NPCCharacter.spc_notable_darshi_1b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDEdEC5703}NPCCharacter.spc_notable_darshi_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD9b306F38}NPCCharacter.spc_notable_darshi_2b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDc0Bb23aD}NPCCharacter.spc_notable_darshi_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD49b695a1}NPCCharacter.spc_notable_darshi_3b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD52b1E30b}NPCCharacter.spc_notable_darshi_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD4b55822E}NPCCharacter.spc_notable_darshi_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDF9CEac76}NPCCharacter.spc_notable_darshi_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD4c781a49}NPCCharacter.spc_notable_darshi_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDAb94aE89}NPCCharacter.spc_notable_darshi_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD0987A9bF}NPCCharacter.spc_notable_darshi_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDC59a7354}NPCCharacter.spc_notable_darshi_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD01b41677}NPCCharacter.spc_notable_darshi_11</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD91503E04}NPCCharacter.spc_notable_darshi_12</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDE30e1e14}NPCCharacter.spc_darshi_headman_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD64a57f3B}NPCCharacter.spc_darshi_headman_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5695D3b8}NPCCharacter.spc_darshi_headman_3</xsl:attribute>
          </template>
        </notable_and_wanderer_templates>
      </xsl:if>
      <xsl:apply-templates select="node()"/>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="Culture[@id='geroia']">
    <xsl:copy>
      <xsl:apply-templates select="@*"/>
      <xsl:if test="not(notable_and_wanderer_templates)">
        <notable_and_wanderer_templates>
          <template>
            <xsl:attribute name="name">{=IMFRD0c21f091}NPCCharacter.spc_wanderer_geroia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD395Fd0D1}NPCCharacter.spc_wanderer_geroia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDEd9f51dC}NPCCharacter.spc_wanderer_geroia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDa6CC5e95}NPCCharacter.spc_wanderer_geroia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDbe5489b7}NPCCharacter.spc_wanderer_geroia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD2643d013}NPCCharacter.spc_wanderer_geroia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDE7Aa2845}NPCCharacter.spc_wanderer_geroia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD8b3248ec}NPCCharacter.spc_wanderer_geroia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD0227fA22}NPCCharacter.spc_wanderer_geroia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5539098c}NPCCharacter.spc_wanderer_geroia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD7c791968}NPCCharacter.spc_wanderer_geroia_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDe83509a9}NPCCharacter.spc_notable_geroia_0</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD698d55FE}NPCCharacter.spc_notable_geroia_0b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD8d92768d}NPCCharacter.spc_notable_geroia_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD2aAec2EF}NPCCharacter.spc_notable_geroia_1b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD5072Ebcb}NPCCharacter.spc_notable_geroia_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD7F9b20Ca}NPCCharacter.spc_notable_geroia_2b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD4e0c4646}NPCCharacter.spc_notable_geroia_3</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD2DF952be}NPCCharacter.spc_notable_geroia_3b</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDA1AbFc8B}NPCCharacter.spc_notable_geroia_4</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD83F58Bd0}NPCCharacter.spc_notable_geroia_5</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDb85EbB42}NPCCharacter.spc_notable_geroia_6</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD7e3E0a39}NPCCharacter.spc_notable_geroia_7</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDae86492a}NPCCharacter.spc_notable_geroia_8</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDF1D9f9a5}NPCCharacter.spc_notable_geroia_9</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD1318Fb7e}NPCCharacter.spc_notable_geroia_10</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDD30c786C}NPCCharacter.spc_notable_geroia_11</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD9a8f218E}NPCCharacter.spc_notable_geroia_12</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDa2832371}NPCCharacter.spc_geroia_headman_1</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRD9cCB3f2F}NPCCharacter.spc_geroia_headman_2</xsl:attribute>
          </template>
          <template>
            <xsl:attribute name="name">{=IMFRDf067A18e}NPCCharacter.spc_geroia_headman_3</xsl:attribute>
          </template>
        </notable_and_wanderer_templates>
      </xsl:if>
      <xsl:apply-templates select="node()"/>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the male_names element and add new names for darshi culture -->
  <xsl:template match="Culture[@id='geroia']/male_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=IMFRD2fA7ae33}Drosio</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1350f463}Leo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6a5e6cE6}Rivio</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD2726c9E6}Lenize</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD2aAe0A73}Iliro</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD8f397344}Alravo</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDE1A139d2}Porro</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1789811F}Xari</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD26186Fe7}Durusio</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6EEDf4A2}Vecero</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD842b0852}Orice</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDFdB03E45}Quintus</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDE1E8D471}Caro</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD604b6fB6}Todios</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD5995Aa8C}Tiron</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDD78fcfcA}Ravero</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDbe9ddFeF}Cazevi</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDD970C155}Puros</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD3EEdcB4d}Aris</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDEd2eD80A}Theodio</xsl:attribute>
      </name>
    </xsl:copy>
  </xsl:template>
  <!-- Template to match the female_names element and add new names for darshi culture -->
  <xsl:template match="Culture[@id='geroia']/female_names">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <name>
        <xsl:attribute name="name">{=IMFRD87687e1C}Alena</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDec62236f}Iris</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD979ed3eF}Teras</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD6284e2b7}Arbella</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD384f0122}Larsa</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD5d1969A0}Pira</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD95D2288e}Lenia</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1e114a59}Linae</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD11B5d5cB}Yura</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDc0B981e4}Irvana</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD2228e400}Olana</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD473A176e}Irena</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDA7E0C2fb}Evara</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRDB14Da0e2}Emara</xsl:attribute>
      </name>
      <name>
        <xsl:attribute name="name">{=IMFRD1b98D952}Serenia</xsl:attribute>
      </name>
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
