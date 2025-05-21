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
                <xsl:attribute name="name">{=!}Bahramshir</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Shahrvand</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Faramarzad</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ardeshiron</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Darayashan</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Hoshmandar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Turanfar</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Keyvanrooz</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Shaygaran</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Vizaresht</xsl:attribute>
            </name>
 <name>
    <xsl:attribute name="name">{=!}Vizaresht</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Zandavash</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Mehrdast</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Sorushan</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Ormazdian</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Vahramid</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Rustamdar</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Farshadmir</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Kavehzad</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Borzuya</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Javanshir</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Surenbad</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Shapurkan</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Niyozad</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Faravand</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Zartoshan</xsl:attribute>
</name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names for darshi culture -->
    <xsl:template match="Culture[@id='darshi']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
<name>
    <xsl:attribute name="name">{=!}Parishanaz</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Rokhsareh</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Mehrnazir</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Sorayanda</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Golmira</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Zhalehzan</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Nazbanou</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Khosrojin</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Farvaneh</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Shahrdokht</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Yasamehr</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Anahita</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Shirinaz</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Dilafruz</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Mahrokh</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Shetareh</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Rozhimeh</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Shahdaneh</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Nozhan</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Veshtara</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Taraneh</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Banujahan</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Zarnaz</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Shahreza</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Elhamis</xsl:attribute>
</name>
<name>
    <xsl:attribute name="name">{=!}Nasimeh</xsl:attribute>
</name>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="Culture[@id='darshi']">
        <xsl:copy>
            <xsl:apply-templates select="@*"/>
            <xsl:if test="not(notable_and_wanderer_templates)">
                <notable_and_wanderer_templates>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_0</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_1</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_2</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_3</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_4</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_5</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_6</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_7</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_8</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_9</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_darshi_10</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_0</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_0b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_1</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_1b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_2</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_2b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_3</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_3b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_4</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_5</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_6</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_7</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_8</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_9</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_10</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_11</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_darshi_12</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_darshi_headman_1</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_darshi_headman_2</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_darshi_headman_3</xsl:attribute>
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
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_0</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_1</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_2</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_3</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_4</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_5</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_6</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_7</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_8</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_9</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_wanderer_geroia_10</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_0</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_0b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_1</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_1b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_2</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_2b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_3</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_3b</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_4</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_5</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_6</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_7</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_8</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_9</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_10</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_11</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_notable_geroia_12</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_geroia_headman_1</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_geroia_headman_2</xsl:attribute>
                    </template>
                    <template>
                        <xsl:attribute name="name">NPCCharacter.spc_geroia_headman_3</xsl:attribute>
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
                <xsl:attribute name="name">{=!}Drosio</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Leo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Rivio</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Lenize</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Iliro</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Alravo</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Porro</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Xari</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Durusio</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Vecero</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Orice</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Quintus</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Caro</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Todios</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Tiron</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Ravero</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Cazevi</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Puros</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Aris</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Theodio</xsl:attribute>
            </name>
        </xsl:copy>
    </xsl:template>

    <!-- Template to match the female_names element and add new names for darshi culture -->
    <xsl:template match="Culture[@id='geroia']/female_names">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <name>
                <xsl:attribute name="name">{=!}Alena</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Iris</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Teras</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Arbella</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Larsa</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Pira</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Lenia</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Linae</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Yura</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Irvana</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Olana</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Irena</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Evara</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Emara</xsl:attribute>
            </name>
            <name>
                <xsl:attribute name="name">{=!}Serenia</xsl:attribute>
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
