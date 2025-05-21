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
                <xsl:attribute name="name">{=IMFd80d7bfC}Aksel</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFB5Bb8433}Bjorn</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF3C7AaB01}Olafr</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF0938bde5}Helgerad</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF68EC51BF}Ole</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFAdB34D63}Gundar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFb2293132}Haraldr</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF1D603799}Hjalmar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF43283e52}Njal</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFE12069b7}Leif</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFB1A40b3e}Lethwyn</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF4139BcB4}Guthric</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names -->
    <xsl:template match="Culture[@id='nord']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <name>
                <xsl:attribute name="name">{=IMF2F4d167d}Astrith</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFEbCA644A}Kara</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFEd3678ad}Elsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF4211Bc0a}Kelsea</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFA1C53c7c}Freja</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFe4148Afd}Annika</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF27Ad6C3c}Ulla</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF2C69eA61}Kay</xsl:attribute>
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
                <xsl:attribute name="name">{=IMF6b4669c5}Tapio</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF566C5e62}Tuisko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFbc2b32d3}Aamu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFBb15eB06}Aarre</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF40D1Bf1b}Aimo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF24196309}Airi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF6933Bdca}Arvo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF30c7bf4B}Eero</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF3523f125}Heino</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF91D9dD56}Hanno</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFAfDAd7eD}Liro</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF960df7b5}Jalo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF93D200a8}Iskko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF61F4E76C}Joukko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF19B4c30f}Kaapo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFe4E97f37}Keijo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF455e292A}Kauko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFa8DC200C}Lari</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names -->
    <xsl:template match="Culture[@id='vakken']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <name>
                <xsl:attribute name="name">{=IMF7d7CF2C8}Laila</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF7FaB8042}Aira</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF1b5b29C0}Alli</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF95628465}Ella</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF3637fb94}Elina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFEd3678ad}Elsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF4b78E23a}Fanni</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF722E35F7}Hanna</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFe80b068A}Heidi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF1b5b29C0}Alli</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF95628465}Ella</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF3637fb94}Elina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFEd3678ad}Elsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF4b78E23a}Fanni</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF7a6989Aa}Likka</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFaa443775}Lina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFb4Bb714C}Liris</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFE1E58e9a}Lahja</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF7e3F5b3D}Kukka</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF36FA9018}Lauri</xsl:attribute>
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
                <xsl:attribute name="name">{=IMF878b050F}Bahram</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF2Ec1fe71}Shahbur</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF5FEA1d6d}Sargon</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF78a54ffA}Hormizhd</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFbaAa47eB}Ardashar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF73a2a9eb}Kavadh</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFE55Ee7dC}Khosrau</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF59C68612}Balash</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF4BD6Ee46}Sarhad</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFF3555Cc3}Esarhaddon</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF0b22Df68}Oshalimahun</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFeaBcCA8D}Zar-Adan-Ekhi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFAbFE7498}Nabu-Ikbi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFb277ADc8}Kudur-Yutsur</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF4504b5b2}Nabu-Ekhi-Erba</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFd6763525}Kanun</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFCd4343a3}Mannu-Ki-Ashur</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFbeCEe190}Namtar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF7e3AFa1d}Nergal-Edir</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF58bA56B1}Nergal-Sar-Ussr</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF3147041A}Nergiglissar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFdeDD6686}Nidintabel</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF39E44889}Shagaraktiyash</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF52C5e695}Sisuthrus</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFC5029454}Kasu-Nadin-Ahu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF0c0a217c}Merodak-Mu-Basa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF5FbAC729}Misa-Nana-Kal-Ammi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFd0E07f46}Mulaghunnuna</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFdc3a34eC}Ul-Khum-Zabu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF5DF69b60}Idadu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF792Ed8F1}Nabonahid</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFD9E4F2c0}Charbil</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFDd0ce5d4}Sayad</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF99615964}Khamis</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFea310986}Zeyno</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF48F5F5aF}Adrahasis</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF38BF09FE}Annunitum</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF2Cca2480}Akku</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF0d8a4365}Furqan</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF9683aef2}Farzad</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names for darshi culture -->
    <xsl:template match="Culture[@id='darshi']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <name>
                <xsl:attribute name="name">{=IMFb4F90D11}Hedu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFe4ECCe09}Sharukina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF24083E91}Ashurina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF85B804eD}Arbella</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFFf102F20}Larsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF6e15A4fA}Deqlat</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF196d478C}Sahdah</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFFfAcDd2C}Nviya</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF35cb1A11}Nahrain</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF9e23F83d}Tauno</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF3c1363dD}Khaya</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF26A9b1d4}Warda</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF5f18857f}Ramina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFfa00599d}Warduni</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF91F740d3}Ilona</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFB1DF3239}Neesan</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF10e3c913}Nuri-Shapira</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF10293621}Dawa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF2355f417}En-hedu-anna</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFEd0bf106}Naram-sin</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF437B9b77}Tashlultum</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMFD3F88B9C}Agusaya</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=IMF254beE19}Raga</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to add notable_and_wanderer_templates element within the darshi culture if it doesn't already exist -->
    <xsl:template match="Culture[@id='darshi']">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:if test="not(notable_and_wanderer_templates)">
                <notable_and_wanderer_templates>
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_0" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_1" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_2" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_3" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_4" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_5" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_6" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_7" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_8" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_9" />
                    <template
                        name="NPCCharacter.spc_wanderer_aserai_10" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_0" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_0b" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_1" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_1b" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_2" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_2b" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_3" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_3b" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_4" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_5" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_6" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_7" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_8" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_9" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_10" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_11" />
                    <template
                        name="NPCCharacter.spc_notable_aserai_12" />
                    <template
                        name="NPCCharacter.spc_aserai_headman_1" />
                    <template
                        name="NPCCharacter.spc_aserai_headman_2" />
                    <template
                        name="NPCCharacter.spc_aserai_headman_3" />
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
