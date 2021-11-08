# CML For Chem4Word
This document outlines how CML is used in Chem4Word, including any special cases.

It also describes the use of custom tags in the **c4w** namespace. There will be a lot of new tags and elements in reaction handling.

## Namespaces
Namespaces currently in use are: 
```csharp
public static XNamespace cml = "http://www.xml-cml.org/schema";

public static XNamespace cmlDict = "http://www.xml-cml.org/dictionary/cml/";

public static XNamespace nameDict = "http://www.xml-cml.org/dictionary/cml/name/";

public static XNamespace conventions = "http://www.xml-cml.org/convention/";

public static XNamespace c4w = "http://www.chem4word.com/cml";

```
### Usage
We will default where possible to established namespaces to handle reaction specific elements. As XML tags can be arbitrarily nested regardless of namespace, it makes sense to use existing standards where possible:
## Reaction Elements
We will use the existing CML-React vocabulary to describe reactions. This is standard within the CML namespace:
```xml
<reactionScheme id="rs1"> <!--one and ONLY one of these ! -->
    <reaction id="r1"> <!-- one or many -->
        <reactantList> <!-- one -->
            <reactant ref="m1"/>
            <reactant ref="m3" title="mCPBA" role="reagent"/>
        </reactantList>
        <productList> <!-- one -->
            <product ref="m2"/>
        </productList>
        <substanceList> <!-- one -->
            <substance role="solvent" dictRef="smlSolvent"/>
        </substanceList>
    </reaction>
</reactionScheme>
.
.
.
<molecule id="m3">
<!-- structural definition of mCPBA -->
</molecule>
....
```
Where reagent and conditions are specified by free text above and below the arrow, we will use custom **c4w:reagents** and **c4w:conditions** elements:
```xml
<reactionScheme id="rs1">
    <reaction id="r1">
        <reactantList>
            <reactant ref="m1"/>
            <reactant ref="m3" title="mCPBA" role="reagent"/>
        </reactantList>
        <productList>
            <product ref="m2"/>
        </productList>
        <substanceList>
            <substance role="solvent" dictRef="smlSolvent"/>
        </substanceList>
        <c4w:reagents>reagent text goes here</c4w:reagents>
        <c4w:conditions>conditions text goes here</c4w:conditions>
    </reaction>
</reactionScheme>
.
.
.
<molecule id="m3">
<!-- structural definition of mCPBA -->
</molecule>
....
```
Both these elements will contain complex content, not plain text. To display formatted text we will nest a single **FlowDocument** tag in the standard XAML namespace http://schemas.microsoft.com/winfx/2006/xaml/presentation. The FlowDocument will contain a single **Paragraph** tag. This nesting will aid direct loading into a RichTextBox. So a reaction specfication containing formatted text will look like:
```xml
<reactionScheme id="rs1">
    <reaction id="r1">
        <reactantList>
            <reactant ref="m1"/>
            <reactant ref="m3" title="mCPBA" role="reagent"/>
        </reactantList>
        <productList>
            <product ref="m2"/>
        </productList>
        <substanceList>
            <substance role="solvent" dictRef="smlSolvent"/>
        </substanceList>
        <c4w:reagents>
            <FlowDocument xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Paragraph>
                  <Bold>Some text in bold</Bold> some text that is not bold
                </Paragraph>
            </FlowDocument>
        </c4w:reagents>
        <c4w:conditions>conditions text goes here</c4w:conditions>
    </reaction>
</reactionScheme>
.
.
.
<molecule id="m3">
<!-- structural definition of mCPBA -->
</molecule>
....
```

## Notes on Tags and Attributes
### reactionScheme
For now, there will be _one_ of these per model.  
### c4w:reagents / cw4:conditions
One per reaction. Will always contain a **FlowDocument** as root. For now just supports formatted text
### c4w:textRef
Attribute applied to **reactant** & **substance** tags to allow back referencing from **Runs** of formatted text. Not currently used. 
Example use:
```xml
<reactionScheme id="rs1">
    <reaction id="r1">
        <reactantList>
            <reactant ref="m1"/>
            <reactant ref="m3" title="mCPBA" role="reagent"/>
        </reactantList>
        <productList>
            <product ref="m2"/>
        </productList>
        <substanceList>
            <substance role="solvent" dictRef="smlSolvent"/>
        </substanceList>
        <c4w:reagents>
            <FlowDocument xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Paragraph>
                  <Run c4w:textRef="m3">m-CPBA</Run>
                </Paragraph>
            </FlowDocument>
        </c4w:reagents>
        <c4w:conditions>conditions text goes here</c4w:conditions>
    </reaction>
</reactionScheme>
.
.
.
<molecule id="m3">
<!-- structural definition of mCPBA -->
</molecule>
....
```
### c4w:molRef
Element that denotes an 'inlined' structure within the reagents or conditions text. References a molecule via the **ref** attribute or embeds the molecule directly:
```xml
<c4w:reagents>
    <FlowDocument xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
        <Paragraph>
            <Run c4w:textRef="m3">m-CPBA</Run>
            <c4w:molRef ref="m3"/>
        </Paragraph>
    </FlowDocument>
</c4w:reagents>
```

```xml
<c4w:reagents>
    <FlowDocument xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
        <Paragraph>
            <Run c4w:textRef="m3">m-CPBA</Run>
            <c4w:molRef>
                <cml:molecule>
                ...
                </cml:molecule>
            </c4w:molRef>
        </Paragraph>
    </FlowDocument>
</c4w:reagents>
```
The second  case is preferred as it avoids the ambiguity that arises from a structure being included as a molecule in its own right, or as an embedded reagent. We will adhere to this convention.
### c4W:arrowStart / c4w:arrowEnd
Attributes applied to **reaction** element that describe the physical layout of the reaction. Expressed in Cartesian coordinates (x, y):
```xml
<reaction id="r1" reactionType="reversible" 
cml:arrowStart="100,100" cml:arrowEnd="300,100" /> <!-- even equilibrium, arrow fixed -->
```
### c4w:textOffset
Attribute applied to the **c4w:reagents** and **c4w:conditions** elements to locate the centre of the text panels. Expressed in polar coordinates scaled to the arrow length, not Cartesian coordinates (fractional length along arrow, angle), and relative to the arrow start. This means that even if the arrow is rotated, the coordinates should be unchanged. If absent, then default placement will be used.
```xml
<c4w:reagents c4w:textOffset="0.5, 30">
            <FlowDocument xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Paragraph>
```
