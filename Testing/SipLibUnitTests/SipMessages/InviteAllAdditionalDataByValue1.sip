INVITE urn:service:sos SIP/2.0
To: <sip:911@10.1.11.170>
Via: SIP/2.0/TCP 10.1.11.3:5060;branch=z9hG4bK6dcca765fea14058a0fc288bc642c38e
From: <sip:8185553333@10.1.6.110>;tag=4f922ff33ab846778e7555d020d16453
Call-ID: 6223979ff9574ec695d4a10628d06a99
Contact: <sip:8185553333@10.1.11.3;transport=tcp>
Call-Info: <urn:nena:uid:incidentid:ea4c58330f2a44019c534515e3592037:AddDataClient@10.1.11.3>; purpose=nena-IncidentId
Call-Info: <urn:nena:uid:callid:c7bb06a82d274abbb7cd73ca1f3167f7:AddDataClient@10.1.11.3>; purpose=nena-CallId
Max-Forwards: 70
CSeq: 1378872421 INVITE
Allow: ACK, BYE, CANCEL, INFO, INVITE, NOTIFY, OPTIONS, REFER
Geolocation: <cid:8185553333@10.1.11.3>;inserted-by=AddDataClient
Call-Info: <cid:ProviderInfo@AddDataClient.com>;purpose=EmergencyCallData.ProviderInfo
Call-Info: <cid:ServiceInfo@AddDataClient.com>;purpose=EmergencyCallData.ServiceInfo
Call-Info: <cid:DeviceInfo@AddDataClient.com>;purpose=EmergencyCallData.DeviceInfo
Call-Info: <cid:SubscriberInfo@AddDataClient.com>;purpose=EmergencyCallData.Subscriber
Call-Info: <cid:Comment@AddDataClient.com>;purpose=EmergencyCallData.Comment
Call-Info: <cid:NENA-CallerInfo@AddDataClient.com>;purpose=EmergencyCallData.NENA-CallerInfo
Call-Info: <cid:NENA-LocationInfo@AddDataClient.com>;purpose=EmergencyCallData.NENA-LocationInfo
Content-Type: multipart/mixed;boundary=boundary1
Content-Length: 20252

--boundary1
Content-Type: application/sdp

v=0
o=- 398805620 1 IN IP4 10.1.11.3
s=Moducom_AddDataClient
c=IN IP4 10.1.11.3
t=0 0
m=audio 20014 RTP/AVP 0
a=rtpmap:0 PCMU/8000
a=sendrecv


--boundary1
Content-Type: application/pidf+xml
Content-ID: <8185553333@10.1.11.3>

<presence xmlns="urn:ietf:params:xml:ns:pidf" xmlns:gp="urn:ietf:params:xml:ns:pidf:geopriv10"
    xmlns:gbp="urn:ietf:params:xml:ns:pidf:geopriv10:basicPolicy"
    xmlns:dm="urn:ietf:params:xml:ns:pidf:data-model">
    <dm:device id="target123-1">
        <gp:geopriv>
            <gp:location-info>
                <civicAddress xmlns="urn:ietf:params:xml:ns:pidf:geopriv10:civicAddr">
                    <country>US</country>
                    <A1>CA</A1>
                    <A3>Simi Valley</A3>
                    <RD>Stonebrook</RD>
                    <STS>Street</STS>
                    <HNO>311</HNO>
                    <PC>93065</PC>
                </civicAddress>
            </gp:location-info>
            <gp:usage-rules>
                <gbp:retransmission-allowed>true</gbp:retransmission-allowed>
                <gbp:retention-expiry>2016-12-10T20:00:00Z </gbp:retention-expiry>
            </gp:usage-rules>
            <gp:method>802.11</gp:method>
        </gp:geopriv>
        <dm:deviceID>mac:00-0d-4b-30-72-df</dm:deviceID>
        <dm:timestamp>2015-07-09T20:57:29Z</dm:timestamp>
    </dm:device>
    <dm:person id="12345">
      <gp:geopriv>
        <gp:location-info>
          <Circle xmlns="http://www.opengis.net/gml" srsName="urn:ogc:def:crs:EPSG::4326">
            <pos>34.247493 -118.791885</pos>
            <radius uom="urn:ogc:def:uom:EPSG::9001">
              50
            </radius>
          </Circle>
        </gp:location-info>
        <gp:usage-rules>
          <gbp:retransmission-allowed>true</gbp:retransmission-allowed>
          <gbp:retention-expiry>2016-12-10T20:00:00Z </gbp:retention-expiry>
        </gp:usage-rules>
        <gp:method>802.11</gp:method>
      </gp:geopriv>
    </dm:person>
</presence>

--boundary1
Content-Type: application/EmergencyCallData.ProviderInfo+xml
Content-ID: <ProviderInfo@AddDataClient.com>

<ad:EmergencyCallData.ProviderInfo xmlns:ad="urn:ietf:params:xml:ns:EmergencyCallData:ProviderInfo">
    <ad:DataProviderReference>string0987654321@example.org </ad:DataProviderReference>
    <ad:DataProviderString>Smith Telecom Inc.</ad:DataProviderString>
    <ad:ProviderID>urn:nena:companyid:ID123</ad:ProviderID>
    <ad:ProviderIDSeries>NENA</ad:ProviderIDSeries>
    <ad:TypeOfProvider>Telecom Provider</ad:TypeOfProvider>
    <ad:ContactURI>tel:+1-201-555-0123</ad:ContactURI>
    <ad:Language>en</ad:Language>
    <ad:DataProviderContact xmlns="urn:ietf:params:xml:ns:vcard-4.0">
        <vcard>
            <fn>
                <text>John Smith</text>
            </fn>
            <n>
                <surname>Smith</surname>
                <given>John</given>
                <additional/>
                <prefix/>
                <suffix>ENP</suffix>
            </n>
            <bday>
                <date>--0203</date>
            </bday>
            <anniversary>
                <date-time>20090808T1430-0500</date-time>
            </anniversary>
            <gender>
                <sex>M</sex>
            </gender>
            <lang>
                <parameters>
                    <pref>
                        <integer>1</integer>
                    </pref>
                </parameters>
                <language-tag>en</language-tag>
            </lang>
            <lang>
                <parameters>
                    <pref>
                        <integer>2</integer>
                    </pref>
                </parameters>
                <language-tag>de</language-tag>
            </lang>
            <org>
                <parameters>
                    <type>
                        <text>work</text>
                    </type>
                </parameters>
                <text>Example VoIP Provider</text>
            </org>
            <adr>
                <parameters>
                    <type>
                        <text>work</text>
                    </type>
                    <label>
                        <text>John Smith
123 First St.
New York, NY 12345</text>
                    </label>
                </parameters>
                <pobox/>
                <ext/>
                <street>123 First St.</street>
                <locality>New York</locality>
                <region>NY</region>
                <code>12345</code>
                <country>USA</country>
            </adr>
            <tel>
                <parameters>
                    <type>
                        <text>work</text>
                        <text>voice</text>
                    </type>
                </parameters>
                <uri>tel:+1 800-123-4567</uri>
            </tel>
            <email>
                <parameters>
                    <type>
                        <text>work</text>
                    </type>
                </parameters>
                <text>John.Smith@SmithTelecom.com</text>
            </email>
            <geo>
                <parameters>
                    <type>
                        <text>work</text>
                    </type>
                </parameters>
                <uri>geo:60.210796,24.812924</uri>
            </geo>
            <key>
                <parameters>
                    <type>
                        <text>work</text>
                    </type>
                </parameters>
                <uri>http://www.SmithTelecom.com/key.asc </uri>
            </key>
            <tz>
                <text>America/Eastern</text>
            </tz>
            <url>
                <parameters>
                    <type>
                        <text>work</text>
                    </type>
                </parameters>
                <uri>http://www.SmithTelecom.com</uri>
            </url>
        </vcard>
    </ad:DataProviderContact>
</ad:EmergencyCallData.ProviderInfo>

--boundary1
Content-Type: application/EmergencyCallData.ServiceInfo+xml
Content-ID: <ServiceInfo@AddDataClient.com>

<svc:EmergencyCallData.ServiceInfo xmlns:svc="urn:ietf:params:xml:ns:EmergencyCallData:ServiceInfo">
    <svc:DataProviderReference>2468.IBOC.MLTS.1359@example.org </svc:DataProviderReference>
    <svc:ServiceEnvironment>Business</svc:ServiceEnvironment>
    <svc:ServiceType>MLTS-hosted</svc:ServiceType>
    <svc:ServiceMobility>Fixed</svc:ServiceMobility>
</svc:EmergencyCallData.ServiceInfo>

--boundary1
Content-Type: application/EmergencyCallData.DeviceInfo+xml
Content-ID: <DeviceInfo@AddDataClient.com>

<dev:EmergencyCallData.DeviceInfo xmlns:dev="urn:ietf:params:xml:ns:EmergencyCallData:DeviceInfo">
    <dev:DataProviderReference>d4b3072df.201409182208075@example.org </dev:DataProviderReference>
    <dev:DeviceClassification>fixed</dev:DeviceClassification>
    <dev:DeviceMfgr>Nokia</dev:DeviceMfgr>
    <dev:DeviceModelNr>Lumia 800</dev:DeviceModelNr>
    <dev:UniqueDeviceID TypeOfDeviceID="IMEI">35788104 </dev:UniqueDeviceID>
</dev:EmergencyCallData.DeviceInfo>

--boundary1
Content-Type: application/EmergencyCallData.SubscriberInfo+xml
Content-ID: <SubscriberInfo@AddDataClient.com>

<EmergencyCallData.SubscriberInfo xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" privacyRequested="false" xmlns="urn:ietf:params:xml:ns:EmergencyCallData:SubscriberInfo">
  <DataProviderReference>FEABFECD901@example.org</DataProviderReference>
  <SubscriberData>
    <vcard xmlns="urn:ietf:params:xml:ns:vcard-4.0">
      <adr>
        <parameters>
          <type>
            <text>home</text>
          </type>
          <label>
            <text>Bill Jones
311 Stonebrook St.
Simi Valley, CA 93065</text>
          </label>
        </parameters>
        <pobox />
        <ext />
        <street>311 Stonebrook St.</street>
        <locality>Simi Valley</locality>
        <region>CA</region>
        <code>93065</code>
        <country>USA</country>
      </adr>
      <anniversary>
        <date-time>20090808T1430-0500</date-time>
      </anniversary>
      <bday>
        <date>--0203</date>
      </bday>
      <email>
        <parameters>
          <type>
            <text>home</text>
          </type>
        </parameters>
        <text>BillJones@hotmail.com</text>
      </email>
      <email>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <text>BillJones@abc.com</text>
      </email>
      <fn>
        <text>Bill Jones</text>
      </fn>
      <geo>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <uri>geo:46.766336,-71.28955</uri>
      </geo>
      <key>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <uri>http://www.viagenie.ca/Bill.Jones/Jones.asc</uri>
      </key>
      <lang>
        <parameters>
          <pref>
            <integer>1</integer>
          </pref>
        </parameters>
        <language-tag>en</language-tag>
      </lang>
      <lang>
        <parameters>
          <pref>
            <integer>2</integer>
          </pref>
        </parameters>
        <language-tag>fr</language-tag>
      </lang>
      <n>
        <surname>Jones</surname>
        <given>Bill</given>
        <additional />
        <prefix />
      </n>
      <org>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <text>Viagenie</text>
      </org>
      <gender>
        <sex>M</sex>
      </gender>
      <tel>
        <parameters>
          <type>
            <text>home</text>
            <text>voice</text>
          </type>
        </parameters>
        <uri>tel:+1 818 555 3333</uri>
      </tel>
      <tel>
        <parameters>
          <type>
            <text>cell</text>
          </type>
        </parameters>
        <uri>tel:+1 818 444 1212</uri>
      </tel>
      <tz>
        <text>America/Pacific</text>
      </tz>
      <url>
        <parameters>
          <type>
            <text>home</text>
          </type>
        </parameters>
        <uri>http://nomis80.org</uri>
      </url>
    </vcard>
  </SubscriberData>
</EmergencyCallData.SubscriberInfo>
--boundary1
Content-Type: application/EmergencyCallData.Comment+xml
Content-ID: <Comment@AddDataClient.com>

<com:EmergencyCallData.Comment xmlns:com="urn:ietf:params:xml:ns:EmergencyCallData:Comment">
    <com:DataProviderReference>string0987654321@example.org </com:DataProviderReference>
    <com:Comment xml:lang="en">This is an example of a comment.</com:Comment>
</com:EmergencyCallData.Comment>

--boundary1
Content-Type: application/EmergencyCallData.NENA-CallerInfo+xml
Content-ID: <NENA-CallerInfo@AddDataClient.com>

<!--Sample XML file generated by XMLSpy v2014 rel. 2 sp1 (http://www.altova.com)-->
<nena-callerinfo:EmergencyCallData.NENA-CallerInfo xmlns="urn:ietf:params:xml:ns:vcard-4.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:nena-callerinfo="urn:nena:xml:ns:EmergencyCallData:CallerInfo" xmlns:n1="http://www.altova.com/samplexml/other-namespace">
	<nena-callerinfo:DataProviderReference>string0987654321@example.org</nena-callerinfo:DataProviderReference>
	<nena-callerinfo:CallerData>
    <vcard xmlns="urn:ietf:params:xml:ns:vcard-4.0">
      <adr>
        <parameters>
          <type>
            <text>home</text>
          </type>
          <label>
            <text>Bill Jones
311 Stonebrook St.
Simi Valley, CA 93065</text>
          </label>
        </parameters>
        <pobox />
        <ext />
        <street>311 Stonebrook St.</street>
        <locality>Simi Valley</locality>
        <region>CA</region>
        <code>93065</code>
        <country>USA</country>
      </adr>
      <anniversary>
        <date-time>20090808T1430-0500</date-time>
      </anniversary>
      <bday>
        <date>--0203</date>
      </bday>
      <email>
        <parameters>
          <type>
            <text>home</text>
          </type>
        </parameters>
        <text>BillJones@hotmail.com</text>
      </email>
      <email>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <text>BillJones@abc.com</text>
      </email>
      <fn>
        <text>Bill Jones</text>
      </fn>
      <geo>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <uri>geo:46.766336,-71.28955</uri>
      </geo>
      <key>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <uri>http://www.viagenie.ca/Bill.Jones/Jones.asc</uri>
      </key>
      <lang>
        <parameters>
          <pref>
            <integer>1</integer>
          </pref>
        </parameters>
        <language-tag>en</language-tag>
      </lang>
      <lang>
        <parameters>
          <pref>
            <integer>2</integer>
          </pref>
        </parameters>
        <language-tag>fr</language-tag>
      </lang>
      <n>
        <surname>Jones</surname>
        <given>Bill</given>
        <additional />
        <prefix />
      </n>
      <org>
        <parameters>
          <type>
            <text>work</text>
          </type>
        </parameters>
        <text>Viagenie</text>
      </org>
      <gender>
        <sex>M</sex>
      </gender>
      <tel>
        <parameters>
          <type>
            <text>home</text>
            <text>voice</text>
          </type>
          <pref>
              <integer>2</integer>
          </pref>
        </parameters>
        <uri>tel:+1 818 555 3333</uri>
      </tel>
      <tel>
        <parameters>
          <type>
            <text>cell</text>
          </type>
          <pref>
              <integer>1</integer>
          </pref>
        </parameters>
        <uri>tel:+1 818 444 1212</uri>
      </tel>
      <tz>
        <text>America/Pacific</text>
      </tz>
      <url>
        <parameters>
          <type>
            <text>home</text>
          </type>
        </parameters>
        <uri>http://BillJones.com</uri>
      </url>
      <note><text>Here is a note about the caller.</text></note>
      <note><text>Here is another note about the caller.</text></note>

    <group name="Emergency Contact">
        <fn>
            <text>Sally Jones</text>
        </fn>
        <related>
            <parameters>
                <type>
                    <text>spouse</text>
                </type>
            </parameters>
            <text>Primary Emergency Contact</text>
        </related>
        <tel>
            <parameters>
                <type>
                    <text>home</text>
                    <text>voice</text>
                </type>
                <pref>
                    <integer>1</integer>
                </pref>
            </parameters>
            <uri>tel:+1 818 555 3333</uri>
        </tel>
        <lang>
            <parameters>
                <pref>
                    <integer>1</integer>
                </pref>
            </parameters>
            <language-tag>en</language-tag>
        </lang>
        <lang>
            <parameters>
                <pref>
                    <integer>2</integer>
                </pref>
            </parameters>
            <language-tag>fr</language-tag>
        </lang>
        <adr>
            <parameters>
                <type>
                    <text>home</text>
                </type>
            </parameters>
            <pobox />
            <ext />
            <street>311 Stonebrook St.</street>
            <locality>Simi Valley</locality>
            <region>CA</region>
            <code>93065</code>
            <country>USA</country>
        </adr>

        <note><text>Usually at home during the day.</text></note>

    </group>
    </vcard>
	</nena-callerinfo:CallerData>
	<nena-callerinfo:CallerSpecificData>http://10.1.11.3/AdditionalLocationData/8185553333/CallerData.html</nena-callerinfo:CallerSpecificData>
	<nena-callerinfo:CallerSpecificType>HTML</nena-callerinfo:CallerSpecificType>
</nena-callerinfo:EmergencyCallData.NENA-CallerInfo>

--boundary1
Content-Type: application/EmergencyCallData.NENA-LocationInfo+xml
Content-ID: <NENA-LocationInfo@AddDataClient.com>

<!--Sample XML file generated by XMLSpy v2014 rel. 2 sp1 (http://www.altova.com)-->
<nena-locinfo:EmergencyCallData.NENA-LocationInfo xmlns="urn:ietf:params:xml:ns:vcard-4.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:nena-locinfo="urn:nena:xml:ns:EmergencyCallData:LocationInfo" xmlns:n1="http://www.altova.com/samplexml/other-namespace" xsi:schemaLocation="urn:nena:xml:ns:EmergencyCallData:LocationInfo ..\2.0\LocationInfo.xsd">
	<nena-locinfo:DataProviderReference>string0987654321@example.org</nena-locinfo:DataProviderReference>
	<nena-locinfo:LocationContacts>
		<vcard>
			<fn>
				<text>Hannes Tschofenig</text>
			</fn>
			<n>
				<surname>Hannes</surname>
				<given>Tschofenig</given>
				<additional/>
				<prefix/>
				<suffix>Dipl. Ing.</suffix>
			</n>
			<bday>
				<date>--0203</date>
			</bday>
			<anniversary>
				<date-time>20090808T1430-0500</date-time>
			</anniversary>
			<gender>
				<sex>M</sex>
			</gender>
			<lang>
				<parameters>
					<pref>
						<integer>1</integer>
					</pref>
				</parameters>
				<language-tag>de</language-tag>
			</lang>
			<lang>
				<parameters>
					<pref>
						<integer>2</integer>
					</pref>
				</parameters>
				<language-tag>en</language-tag>
			</lang>
			<org>
				<parameters>
					<type>
						<text>work</text>
					</type>
				</parameters>
				<text>Example VoIP Provider</text>
			</org>
			<adr>
				<parameters>
					<type>
						<text>work</text>
					</type>
					<label>
						<text>Hannes Tschofenig
Linnoitustie 6
Espoo , Finland
02600</text>
					</label>
				</parameters>
				<pobox/>
				<ext/>
				<street>Linnoitustie 6</street>
				<locality>Espoo</locality>
				<region>Uusimaa</region>
				<code>02600</code>
				<country>Finland</country>
			</adr>
			<tel>
				<parameters>
					<type>
						<text>work</text>
						<text>voice</text>
					</type>
				</parameters>
				<uri>tel:+358 50 4871445</uri>
			</tel>
			<tel>
				<parameters>
					<type>
						<text>work</text>
						<text>main-number</text>
						<text>voice</text>
					</type>
				</parameters>
				<uri>tel:+358 50 5050505</uri>
			</tel>
			<email>
				<parameters>
					<type>
						<text>work</text>
					</type>
				</parameters>
				<text>hannes.tschofenig@nsn.com</text>
			</email>
			<geo>
				<parameters>
					<type>
						<text>work</text>
					</type>
				</parameters>
				<uri>geo:60.210796,24.812924</uri>
			</geo>
			<key>
				<parameters>
					<type>
						<text>home</text>
					</type>
				</parameters>
				<uri>
http://www.tschofenig.priv.at/key.asc
</uri>
			</key>
			<tz>
				<text>Finland/Helsinki</text>
			</tz>
			<url>
				<parameters>
					<type>
						<text>home</text>
					</type>
				</parameters>
				<uri>http://www.tschofenig.priv.at</uri>
			</url>
		</vcard>
	</nena-locinfo:LocationContacts>
	<nena-locinfo:LocationSpecificData>http://10.1.11.3/AdditionalLocationData/8185553333/8185553333.html</nena-locinfo:LocationSpecificData>
	<nena-locinfo:LocationSpecificType>HTTP URL</nena-locinfo:LocationSpecificType>
</nena-locinfo:EmergencyCallData.NENA-LocationInfo>

--boundary1--
