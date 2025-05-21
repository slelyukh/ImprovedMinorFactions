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
                <xsl:attribute name="name">{=!}Aksel</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Bjorn</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Olafr</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Helgerad</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ole</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Gundar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Haraldr</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Hjalmar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Njal</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Leif</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Lethwyn</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Guthric</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names -->
    <xsl:template match="Culture[@id='nord']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <name>
                <xsl:attribute name="name">{=!}Astrith</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kara</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Elsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kelsea</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Freja</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Annika</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ulla</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kay</xsl:attribute>
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
                <xsl:attribute name="name">{=!}Tapio</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Tuisko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Aamu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Aarre</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Aimo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Airi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Arvo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Eero</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Heino</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Hanno</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Liro</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Jalo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Iskko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Joukko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kaapo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Keijo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kauko</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Lari</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names -->
    <xsl:template match="Culture[@id='vakken']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <name>
                <xsl:attribute name="name">{=!}Laila</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Aira</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Alli</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ella</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Elina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Elsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Fanni</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Hanna</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Heidi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Alli</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ella</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Elina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Elsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Fanni</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Likka</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Lina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Liris</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Lahja</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kukka</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Lauri</xsl:attribute>
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
                <xsl:attribute name="name">{=!}Bahram</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Shahbur</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Sargon</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Hormizhd</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ardashar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kavadh</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Khosrau</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Balash</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Sarhad</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Esarhaddon</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Oshalimahun</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Zar-Adan-Ekhi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nabu-Ikbi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kudur-Yutsur</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nabu-Ekhi-Erba</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kanun</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Mannu-Ki-Ashur</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Namtar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nergal-Edir</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nergal-Sar-Ussr</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nergiglissar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nidintabel</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Shagaraktiyash</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Sisuthrus</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Kasu-Nadin-Ahu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Merodak-Mu-Basa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Misa-Nana-Kal-Ammi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Mulaghunnuna</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ul-Khum-Zabu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Idadu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nabonahid</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Charbil</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Sayad</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Khamis</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Zeyno</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Adrahasis</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Annunitum</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Akku</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Furqan</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Farzad</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names for darshi culture -->
    <xsl:template match="Culture[@id='darshi']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <name>
                <xsl:attribute name="name">{=!}Hedu</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Sharukina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ashurina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Arbella</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Larsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Deqlat</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Sahdah</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nviya</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nahrain</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Tauno</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Khaya</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Warda</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ramina</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Warduni</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ilona</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Neesan</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Nuri-Shapira</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Dawa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}En-hedu-anna</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Naram-sin</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Tashlultum</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Agusaya</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Raga</xsl:attribute>
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
