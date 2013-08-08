using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;
using System.Configuration.Assemblies;
using System.Configuration;
using System.Xml;

namespace NetLog.Logging {
	public class LoggerConfiguration: PropertyConfiguration {
		[ConfigurationProperty("level", DefaultValue = "INFO", IsRequired = true)]
		public string Level {
			get { return (string)base[ "value" ]; }
			set { base[ "value" ] = value; }
		}
	}
	public class HandlerConfiguration: LoggerConfiguration {
		[ConfigurationProperty("class", DefaultValue = "", IsRequired = true)]
		public string ClassName {
			get { return (string)this[ "class" ]; }
			set { this[ "class" ] = value; }
		}
	}
	public class PropertyConfiguration: ConfigurationElement {
		[ConfigurationProperty("name", DefaultValue = "", IsRequired = true)]
		public string Name {
			get { return (string)this[ "name" ]; }
			set { this[ "name" ] = value; }
		}
		[ConfigurationProperty("value", DefaultValue = "", IsRequired = true)]
		public string Value {
			get { return (string)this[ "value" ]; }
			set { this[ "value" ] = value; }
		}
	}
	public class FormatterConfiguration: HandlerConfiguration {
		[ConfigurationProperty("handler", DefaultValue = "", IsRequired = true)]
		public string Handler {
			get { return (string)this[ "value" ]; }
			set { this[ "value" ] = value; }
		}
	}
	public class LoggerConfigurationElements: ConfigurationElementCollection {
		public override ConfigurationElementCollectionType CollectionType {
			get {
				return ConfigurationElementCollectionType.AddRemoveClearMap;
			}
		}
		protected override ConfigurationElement CreateNewElement() {
			return new PropertyConfiguration();
		}
		protected override object GetElementKey( ConfigurationElement element ) {
			return ( (PropertyConfiguration)element ).Name;
		}
		public new string AddElementName {
			get { return base.AddElementName; }
			set { base.AddElementName = value; }
		}
		public new string ClearElementName {
			get { return base.ClearElementName; }
			set { base.ClearElementName = value; }

		}

		public new string RemoveElementName {
			get { return base.RemoveElementName; }
		}

		public new int Count {
			get { return base.Count; }
		}


		public LoggerConfigurationElements this[ int index ] {
			get {
				return (LoggerConfigurationElements)BaseGet(index);
			}
			set {
				if( BaseGet(index) != null ) {
					BaseRemoveAt(index);
				}
				BaseAdd(index, value);
			}
		}

		new public LoggerConfigurationElements this[ string Name ] {
			get { return (LoggerConfigurationElements)BaseGet(Name); }
		}

		public int IndexOf( LoggerConfigurationElements exchange ) {
			return BaseIndexOf(exchange);
		}

		public void Add( LoggerConfigurationElements exchange ) {
			BaseAdd(exchange);
			// Add custom code here.
		}

		protected override void BaseAdd( ConfigurationElement element ) {
			BaseAdd(element, false);
			// Add custom code here.
		}

		public void Remove( LoggerConfigurationElements exchange ) {
			if( BaseIndexOf(exchange) >= 0 )
				BaseRemove(exchange);
		}

		public void RemoveAt( int index ) {
			BaseRemoveAt(index);
		}

		public void Remove( string name ) {
			BaseRemove(name);
		}

		public void Clear() {
			BaseClear();
			// Add custom code here.
		}
	}
	

	public class NetLogConfigurationSection: ConfigurationSection {
		[ConfigurationProperty("LoggingConfiguration", IsDefaultCollection = false)]
		public LoggerConfigurationElements LoggingConfiguration {
			get {
				LoggerConfigurationElements configEntries = (LoggerConfigurationElements)this[ "LoggingConfiguration" ];
				return configEntries;
			}
		}
	}
}
